using Polly;
using Polly.Retry;

namespace ConsistencyClass;

using static BillingCycleEvent;

internal class BillingCycleService(
    VirtualCreditCardDatabase virtualCreditCardDatabase,
    BillingCycleDatabase billingCycleDatabase
)
{
    public BillingCycleId? GetCurrentlyOpenedBillingCycleId(CardId cardId) =>
        virtualCreditCardDatabase.Find(cardId).CurrentBillingCycle is { IsOpened: true } currentCycle
            ? currentCycle.Id
            : null;

    public Result OpenNextCycle(CardId cardId)
    {
        var card = virtualCreditCardDatabase.Find(cardId);
        var expectedVersion = card.Version;

        var result = card.OpenNextCycle();

        return result == Result.Success
            ? virtualCreditCardDatabase.Save(card, expectedVersion)
            : result;
    }

    public Result Close(BillingCycleId billingCycleId)
    {
        var billingCycle = billingCycleDatabase.Find(billingCycleId);
        var expectedVersion = billingCycle.Version;

        var result = billingCycle.CloseCycle();

        return result == Result.Success
            ? billingCycleDatabase.Save(billingCycle, expectedVersion)
            : result;
    }
}

internal class BillingCycleEventHandler(
    VirtualCreditCardDatabase virtualCreditCardDatabase,
    BillingCycleDatabase billingCycleDatabase
)
{
    private readonly RetryPolicy<Result> retryPolicy = Policy<Result>
        .Handle<Exception>()
        .OrResult(result => result == Result.Failure)
        .RetryForever();

    public void Handle(object? @event)
    {
        switch (@event)
        {
            case VirtualCreditCardEvent.CycleOpened e:
                OnBillingCycleOpened(e);
                break;

            case CycleClosed e:
                OnBillingCycleClosed(e);
                break;
        }
    }

    private void OnBillingCycleOpened(VirtualCreditCardEvent.CycleOpened cycleOpened)
    {
        var cycle = BillingCycle.OpenCycle(
            cycleOpened.CycleId,
            cycleOpened.CardId,
            cycleOpened.From,
            cycleOpened.To,
            cycleOpened.StartingLimit
        );

        billingCycleDatabase.Save(cycle, 0);
    }

    private void OnBillingCycleClosed(CycleClosed cycleClosed)
    {
        retryPolicy.Execute(() =>
        {
            var card = virtualCreditCardDatabase.Find(cycleClosed.CardId);
            var expectedVersion = card.Version;

            card.RecordCycleClosure(
                cycleClosed.CycleId,
                cycleClosed.ClosingLimit,
                cycleClosed.ClosedAt
            );

            return virtualCreditCardDatabase.Save(card, expectedVersion);
        });
    }
}
