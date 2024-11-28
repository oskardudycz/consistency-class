namespace ConsistencyClass;

using static Result;

internal class VirtualCreditCard
{
    public CardId Id { get; } = CardId.Random();
    public Money AvailableLimit => limit.Available;

    private Limit limit = Limit.Unset;
    private int withdrawalsInCycle;

    private VirtualCreditCard() { }

    public static VirtualCreditCard WithLimit(Money limit)
    {
        var card = new VirtualCreditCard();
        card.AssignLimit(limit);
        return card;
    }

    public Result AssignLimit(Money limit)
    {
        this.limit = Limit.Initial(limit);
        return Success;
    }

    public Result Withdraw(Money amount)
    {
        if (AvailableLimit.IsLessThan(amount))
            return Failure;

        if (withdrawalsInCycle >= 45)
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
