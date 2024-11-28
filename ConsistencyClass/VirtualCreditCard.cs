namespace ConsistencyClass;

using static VirtualCreditCardEvent;
using static Result;

internal class VirtualCreditCard
{
    public CardId Id { get; private set; } = CardId.Empty;
    public Money AvailableLimit => limit.Available;

    private Limit limit = Limit.Unset;
    private int withdrawalsInCycle;
    private readonly List<VirtualCreditCardEvent> pendingEvents = new();

    private VirtualCreditCard() { }

    public static VirtualCreditCard WithLimit(Money limit)
    {
        var cardId = CardId.Random();
        List<VirtualCreditCardEvent> events = [new LimitAssigned(cardId, DateTimeOffset.UtcNow, limit)];
        return Recreate(events);
    }

    public static VirtualCreditCard Recreate(IEnumerable<VirtualCreditCardEvent> stream) =>
        stream.Aggregate(new VirtualCreditCard(), Evolve);

    public static VirtualCreditCard Create(CardId cardId)
    {
        var card = new VirtualCreditCard();
        card.Enqueue(new CardCreated(cardId, DateTimeOffset.UtcNow));
        return card;
    }

    public Result AssignLimit(Money limit) =>
        Success(new LimitAssigned(Id, DateTimeOffset.UtcNow, limit));

    public Result Withdraw(Money amount)
    {
        if (AvailableLimit.IsLessThan(amount))
            return Failure;

        if (withdrawalsInCycle >= 45)
            return Failure;

        return Success(new CardWithdrawn(Id, DateTimeOffset.UtcNow, amount));
    }

    public Result Repay(Money amount) =>
        Success(new CardRepaid(Id, DateTimeOffset.UtcNow, amount));

    public Result CloseCycle() =>
        Success(new CycleClosed(Id, DateTimeOffset.UtcNow));

    public List<VirtualCreditCardEvent> DequeuePendingEvents()
    {
        var result = pendingEvents.ToList();
        pendingEvents.Clear();
        return result;
    }

    private static VirtualCreditCard Evolve(VirtualCreditCard state, VirtualCreditCardEvent evt)
    {
        return evt switch
        {
            CardCreated e => state.Created(e),
            LimitAssigned e => state.LimitAssigned(e),
            CardWithdrawn e => state.CardWithdrawn(e),
            CardRepaid e => state.CardRepaid(e),
            CycleClosed e => state.BillingCycleClosed(e),
            _ => state
        };
    }

    private VirtualCreditCard Created(CardCreated evt)
    {
        Id = evt.CardId;
        return this;
    }

    private VirtualCreditCard LimitAssigned(LimitAssigned evt)
    {
        limit = Limit.Initial(evt.Amount);
        return this;
    }

    private VirtualCreditCard CardWithdrawn(CardWithdrawn evt)
    {
        limit = limit.Use(evt.Amount);
        withdrawalsInCycle++;
        return this;
    }

    private VirtualCreditCard CardRepaid(CardRepaid evt)
    {
        limit = limit.TopUp(evt.Amount);
        return this;
    }

    private VirtualCreditCard BillingCycleClosed(CycleClosed evt)
    {
        withdrawalsInCycle = 0;
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

internal record CardId(Guid Id)
{
    public static CardId Empty => new(Guid.Empty);

    public static CardId Random() => new(Guid.NewGuid());
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

internal record OwnerId(Guid Id)
{
    public static OwnerId Random() => new(Guid.NewGuid());
}

internal record Ownership(HashSet<OwnerId> Owners)
{
    public int Size => Owners.Count;

    public static Ownership Of(params OwnerId[] owners) =>
        new([..owners]);

    public static Ownership Empty() =>
        new([]);

    public bool HasAccess(OwnerId ownerId) =>
        Owners.Contains(ownerId);

    public Ownership AddAccess(OwnerId ownerId)
    {
        var newOwners = new HashSet<OwnerId>(Owners) { ownerId };
        return new Ownership(newOwners);
    }

    public Ownership Revoke(OwnerId ownerId)
    {
        var newOwners = new HashSet<OwnerId>(Owners);
        newOwners.Remove(ownerId);
        return new Ownership(newOwners);
    }
}

internal abstract record VirtualCreditCardEvent
{
    public record CardCreated(CardId CardId, DateTimeOffset CreatedAt): VirtualCreditCardEvent;

    public record CardRepaid(CardId CardId, DateTimeOffset RepaidAt, Money Amount): VirtualCreditCardEvent;

    public record LimitAssigned(CardId CardId, DateTimeOffset AssignedAt, Money Amount): VirtualCreditCardEvent;

    public record CardWithdrawn(CardId CardId, DateTimeOffset WithdrawnAt, Money Amount): VirtualCreditCardEvent;

    public record CycleClosed(CardId CardId, DateTimeOffset ClosedAt): VirtualCreditCardEvent;

    private VirtualCreditCardEvent() { }
}
