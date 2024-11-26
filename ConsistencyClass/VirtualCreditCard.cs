namespace ConsistencyClass;

using static VirtualCreditCardEvent;

public class VirtualCreditCard
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
        stream.Aggregate(new VirtualCreditCard(), (card, evt) => card.Evolve(evt));

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
            return Result.Failure;

        if (withdrawalsInCycle >= 45)
            return Result.Failure;

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

    private VirtualCreditCard Evolve(VirtualCreditCardEvent evt) =>
        evt switch
        {
            CardCreated e => Created(e),
            LimitAssigned e => LimitAssigned(e),
            CardWithdrawn e => CardWithdrawn(e),
            CardRepaid e => CardRepaid(e),
            CycleClosed e => BillingCycleClosed(e),
            _ => this
        };

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
        Evolve(evt);
        pendingEvents.Add(evt);
    }
}

public enum Result
{
    Success,
    Failure
}

public record CardId(Guid Id)
{
    public static CardId Empty => new(Guid.Empty);

    public static CardId Random() => new(Guid.NewGuid());
}

public record Limit(Money Max, Money Used)
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

public record OwnerId(Guid Id)
{
    public static OwnerId Random() => new(Guid.NewGuid());
}

public record Ownership(HashSet<OwnerId> Owners)
{
    public int Size => Owners.Count;

    public static Ownership Of(params OwnerId[] owners) =>
        new(new HashSet<OwnerId>(owners));

    public static Ownership Empty() =>
        new(new HashSet<OwnerId>());

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

public abstract record VirtualCreditCardEvent
{
    public record CardCreated(CardId CardId, DateTimeOffset CreatedAt): VirtualCreditCardEvent;

    public record CardRepaid(CardId CardId, DateTimeOffset RepaidAt, Money Amount): VirtualCreditCardEvent;

    public record LimitAssigned(CardId CardId, DateTimeOffset AssignedAt, Money Amount): VirtualCreditCardEvent;

    public record CardWithdrawn(CardId CardId, DateTimeOffset WithdrawnAt, Money Amount): VirtualCreditCardEvent;

    public record CycleClosed(CardId CardId, DateTimeOffset ClosedAt): VirtualCreditCardEvent;

    private VirtualCreditCardEvent() { }
}