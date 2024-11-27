namespace ConsistencyClass;

using static VirtualCreditCardEvent;


internal abstract record VirtualCreditCardEvent
{
    public record CardCreated(
        CardId CardId,
        CurrencyUnit Currency,
        DateTimeOffset CreatedAt
    ): VirtualCreditCardEvent;

    public record LimitAssigned(
        CardId CardId,
        Money Amount,
        DateTimeOffset AssignedAt
    ): VirtualCreditCardEvent;

    public record CardDeactivated(
        CardId CardId,
        DateTimeOffset DeactivatedAt
    ): VirtualCreditCardEvent;

    public record CycleOpened(
        BillingCycleId CycleId,
        CardId CardId,
        DateTimeOffset From,
        DateTimeOffset To,
        Limit StartingLimit,
        DateTimeOffset OpenedAt
    ): VirtualCreditCardEvent;

    public record CycleClosed(
        BillingCycleId CycleId,
        CardId CardId,
        Money Debt,
        DateTimeOffset ClosedAt
    ): VirtualCreditCardEvent;

    private VirtualCreditCardEvent() { }
}

internal class VirtualCreditCard
{
    public record BillingCycle(BillingCycleId Id, bool IsOpened)
    {
        public static BillingCycle NotExisting = new(BillingCycleId.Empty, false);
    }

    public CardId Id { get; private set; } = CardId.Empty;
    public CurrencyUnit Currency { get; private set; } = CurrencyUnit.Unset;
    public BillingCycle CurrentBillingCycle { get; private set; } = BillingCycle.NotExisting;

    public Limit Limit { get; private set; } = Limit.Unset;
    public Money AvailableLimit => Limit.Available;
    private Money debt = Money.Unset;
    public bool IsActive { get; private set; }
    private readonly List<VirtualCreditCardEvent> pendingEvents = new();

    public int Version { get; private set; }

    private VirtualCreditCard() { }

    public static VirtualCreditCard WithLimit(Money limit)
    {
        var cardId = CardId.Random();
        List<VirtualCreditCardEvent> events =
        [
            new CardCreated(cardId, limit.Currency, DateTimeOffset.UtcNow),
            new LimitAssigned(cardId, limit, DateTimeOffset.UtcNow)
        ];
        return Recreate(events);
    }

    public static VirtualCreditCard Recreate(IEnumerable<VirtualCreditCardEvent> stream) =>
        stream.Aggregate(new VirtualCreditCard(), Evolve);

    public static VirtualCreditCard Create(CardId cardId, CurrencyUnit currency)
    {
        var card = new VirtualCreditCard();
        card.Enqueue(new CardCreated(cardId, currency, DateTimeOffset.UtcNow));
        return card;
    }

    public Result AssignLimit(Money limit) =>
        Success(new LimitAssigned(Id, limit, DateTimeOffset.UtcNow));

    public Result OpenNextCycle()
    {
        if (CurrentBillingCycle.IsOpened)
            return Result.Failure;

        if (!IsActive)
            return Result.Failure;

        var nextCycleId = CurrentBillingCycle != BillingCycle.NotExisting
            ? CurrentBillingCycle.Id.Next()
            : BillingCycleId.FromNow(Id);

        return Success(
            new CycleOpened(
                nextCycleId,
                Id,
                nextCycleId.From,
                nextCycleId.To,
                Limit,
                DateTimeOffset.UtcNow
            )
        );
    }

    // No result, as we just need to accept it
    public void RecordCycleClosure
    (
        BillingCycleId cycleId,
        Limit closingLimit,
        DateTimeOffset closedAt)
    {
        if (CurrentBillingCycle.Id != cycleId)
            return;

        if (!CurrentBillingCycle.IsOpened)
            return;

        var closingDebt = closingLimit.Used;

        Enqueue(
            new CycleClosed(
                cycleId,
                Id,
                closingDebt,
                closedAt
            )
        );

        if (!closingDebt.IsZero)
        {
            Enqueue(
                new CardDeactivated(
                    Id,
                    DateTimeOffset.UtcNow
                )
            );
        }
    }

    public List<VirtualCreditCardEvent> DequeuePendingEvents()
    {
        var result = pendingEvents.ToList();
        pendingEvents.Clear();
        return result;
    }

    private static VirtualCreditCard Evolve(VirtualCreditCard state, VirtualCreditCardEvent evt)
    {
        state.Version++;
        return evt switch
        {
            CardCreated e => state.Created(e),
            LimitAssigned e => state.LimitAssigned(e),
            CycleOpened e => state.CycleOpened(e),
            CycleClosed e => state.CycleClosed(e),
            CardDeactivated e => state.Deactivated(e),
            _ => state
        };
    }

    private VirtualCreditCard Created(CardCreated evt)
    {
        Id = evt.CardId;
        CurrentBillingCycle = BillingCycle.NotExisting;
        IsActive = true;
        Currency = evt.Currency;
        debt = Money.Zero(evt.Currency);
        return this;
    }

    private VirtualCreditCard LimitAssigned(LimitAssigned evt)
    {
        Limit = new Limit(evt.Amount, debt);
        return this;
    }

    private VirtualCreditCard CycleOpened(CycleOpened cycleOpened)
    {
        CurrentBillingCycle = new BillingCycle(cycleOpened.CycleId, true);
        return this;
    }

    private VirtualCreditCard CycleClosed(CycleClosed cycleClosed)
    {
        CurrentBillingCycle = new BillingCycle(cycleClosed.CycleId, false);
        debt = cycleClosed.Debt;
        return this;
    }

    private VirtualCreditCard Deactivated(CardDeactivated deactivated)
    {
        IsActive = false;
        return this;
    }

    private Result Success(VirtualCreditCardEvent evt)
    {
        Enqueue(evt);
        return Result.Success;
    }

    private void Enqueue(VirtualCreditCardEvent evt)
    {
        Evolve(this, evt);
        pendingEvents.Add(evt);
    }
}

internal enum Result
{
    Success,
    Failure
}

public record CardId(Guid ContractId)
{
    public static CardId Empty => new(Guid.Empty);

    public static CardId Random() => new(Guid.NewGuid());

    public override string ToString() =>
        $"Card:{ContractId}";
}

internal record Limit(Money Max, Money Used)
{
    public static readonly Limit Unset = new(Money.Unset, Money.Unset);

    public Money Available => Max.Subtract(Used);

    public static Limit Initial(Money max) => new(max, Money.Zero(max.Currency));

    public Limit Use(Money amount) =>
        this with { Used = Used.Add(amount) };

    public Limit TopUp(Money amount)
    {
        var newUsed = Used.Subtract(amount);
        return this with { Used = newUsed.IsPositiveOrZero ? newUsed : Money.Zero(Max.Currency) };
    }
}
