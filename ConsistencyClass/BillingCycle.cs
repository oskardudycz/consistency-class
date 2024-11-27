namespace ConsistencyClass;

using static BillingCycleEvent;

internal abstract record BillingCycleEvent
{
    public record CycleOpened(
        BillingCycleId CycleId,
        CardId CardId,
        DateTimeOffset From,
        DateTimeOffset To,
        Limit StartingLimit,
        DateTimeOffset OpenedAt
    ): BillingCycleEvent;

    public record CardRepaid(
        BillingCycleId CycleId,
        CardId CardId,
        Money Amount,
        DateTimeOffset RepaidAt
    ): BillingCycleEvent;

    public record CardWithdrawn(
        BillingCycleId CycleId,
        CardId CardId,
        Money Amount,
        DateTimeOffset WithdrawnAt
    ): BillingCycleEvent;

    public record CycleClosed(
        BillingCycleId CycleId,
        CardId CardId,
        Limit ClosingLimit,
        int WithdrawalsInCycle,
        DateTimeOffset ClosedAt
    ): BillingCycleEvent;
}

internal class BillingCycle: IVersioned
{
    private enum Status
    {
        Opened,
        Closed
    }

    public BillingCycleId Id { get; private set; } = BillingCycleId.Empty;
    public Money AvailableLimit => limit.Available;
    private CardId cardId = CardId.Empty;
    private Status status;
    private Limit limit = Limit.Unset;
    private int withdrawalsInCycle;
    private readonly List<BillingCycleEvent> pendingEvents = new();

    public int Version { get; private set; }

    public static BillingCycle WithLimit(Money limit)
    {
        var cardId = CardId.Random();
        var cycleId = BillingCycleId.FromNow(cardId);

        List<BillingCycleEvent> events =
            [new CycleOpened(cycleId, cardId, cycleId.From, cycleId.To, Limit.Initial(limit), DateTimeOffset.UtcNow)];

        return Recreate(events);
    }

    public static BillingCycle Recreate(IEnumerable<BillingCycleEvent> stream) =>
        stream.Aggregate(new BillingCycle(), Evolve);


    public static BillingCycle OpenCycle(BillingCycleId id, CardId cardId, DateTimeOffset from, DateTimeOffset to,
        Limit startingLimit)
    {
        var cycle = new BillingCycle();
        cycle.Success(new CycleOpened(id, cardId, from, to, startingLimit, DateTimeOffset.UtcNow));
        return cycle;
    }

    private BillingCycle Opened(CycleOpened evt)
    {
        Id = evt.CycleId;
        cardId = evt.CardId;
        status = Status.Opened;
        limit = evt.StartingLimit;
        return this;
    }

    public Result CloseCycle()
    {
        if (status == Status.Closed)
            return Result.Failure;

        return Success(new CycleClosed(Id, cardId, limit, withdrawalsInCycle, DateTimeOffset.UtcNow));
    }

    private BillingCycle Closed(CycleClosed evt)
    {
        status = Status.Closed;
        return this;
    }

    public Result Withdraw(Money amount)
    {
        if (status != Status.Opened)
            return Result.Failure;

        if (AvailableLimit.IsLessThan(amount))
            return Result.Failure;

        if (withdrawalsInCycle >= 45)
            return Result.Failure;

        return Success(new CardWithdrawn(Id, cardId, amount, DateTimeOffset.UtcNow));
    }

    private BillingCycle CardWithdrawn(CardWithdrawn evt)
    {
        limit = limit.Use(evt.Amount);
        withdrawalsInCycle++;
        return this;
    }

    public Result Repay(Money amount)
    {
        if (status == Status.Closed)
        {
            // Question: How to handle repaying cycle
            // that was closed without settling all withdrawals?
            return Result.Failure;
        }

        return Success(new CardRepaid(Id, cardId, amount, DateTimeOffset.UtcNow));
    }

    private BillingCycle CardRepaid(CardRepaid evt)
    {
        limit = limit.TopUp(evt.Amount);
        return this;
    }

    private static BillingCycle Evolve(BillingCycle state, BillingCycleEvent evt)
    {
        state.Version++;
        return evt switch
        {
            CycleOpened e => state.Opened(e),
            CardWithdrawn e => state.CardWithdrawn(e),
            CardRepaid e => state.CardRepaid(e),
            CycleClosed e => state.Closed(e),
            _ => state
        };
    }

    public List<BillingCycleEvent> DequeuePendingEvents()
    {
        var result = pendingEvents.ToList();
        pendingEvents.Clear();
        return result;
    }

    public Result Success(BillingCycleEvent evt)
    {
        Enqueue(evt);
        return Result.Success;
    }

    private void Enqueue(BillingCycleEvent evt)
    {
        Evolve(this, evt);
        pendingEvents.Add(evt);
    }
}

internal record BillingCycleId(CardId CardId, DateTimeOffset From, DateTimeOffset To)
{
    public static readonly BillingCycleId Empty = new(CardId.Empty, DateTimeOffset.MinValue, DateTimeOffset.MinValue);

    private static readonly int CycleLength = 30;

    public static BillingCycleId FromNow(CardId cardId)
    {
        var from = DateTimeOffset.UtcNow.Date;
        return new BillingCycleId(cardId, from, from.AddDays(CycleLength));
    }

    public BillingCycleId Next()
    {
        var from = To.AddDays(1);
        return new BillingCycleId(CardId, from, from.AddDays(CycleLength));
    }

    public override string ToString() =>
        $"BillingCycle:{CardId}:{From}:{To}";
}
