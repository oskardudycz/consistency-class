namespace ConsistencyClass;

using static Result;

internal class VirtualCreditCard
{
    public CardId Id { get; } = CardId.Random();
    public Money AvailableLimit => limit.Available;

    private Limit limit = Limit.Unset;
    private int withdrawalsInCycle;
    private Ownership ownership = Ownership.Empty();

    private VirtualCreditCard() { }

    public static VirtualCreditCard WithLimit(Money limit)
    {
        var card = new VirtualCreditCard();
        card.AssignLimit(limit);
        return card;
    }

    public static VirtualCreditCard WithLimitAndOwner(Money limit, OwnerId ownerId)
    {
        var card = WithLimit(limit);
        card.AddAccess(ownerId);
        return card;
    }

    public Result AssignLimit(Money limit)
    {
        this.limit = Limit.Initial(limit);
        return Success;
    }

    public Result Withdraw(Money amount, OwnerId ownerId)
    {
        if (AvailableLimit.IsLessThan(amount))
            return Failure;

        if (withdrawalsInCycle >= 45)
            return Failure;

        if (!ownership.HasAccess(ownerId))
            return Failure;

        limit = limit.Use(amount);
        withdrawalsInCycle++;
        return Success;
    }

    public Result Repay(Money amount)
    {
        limit = limit.TopUp(amount);
        return Success;
    }

    public Result CloseCycle()
    {
        withdrawalsInCycle = 0;
        return Success;
    }

    public Result AddAccess(OwnerId owner)
    {
        if (ownership.Size >= 2)
            return Failure;

        ownership = ownership.AddAccess(owner);
        return Success;
    }

    public Result RevokeAccess(OwnerId owner)
    {
        ownership = ownership.Revoke(owner);
        return Success;
    }
}

public enum Result
{
    Success,
    Failure
}

internal record CardId(Guid Id)
{
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
