namespace ConsistencyClass.Tests;

public class WithdrawingTest
{
    private readonly VirtualCreditCardDatabase creditCardDatabase = new();
    private readonly OwnershipDatabase ownershipDatabase = new();

    private readonly WithdrawService withdrawService;
    private readonly AddLimitService addLimitService;
    private readonly RepayService repayService;
    private readonly CloseCycleService closeCycleService;
    private readonly OwnershipService ownershipService;

    private static readonly OwnerId OSKAR = OwnerId.Random();
    private static readonly OwnerId KUBA = OwnerId.Random();

    public WithdrawingTest()
    {
        withdrawService = new WithdrawService(creditCardDatabase, ownershipDatabase);
        addLimitService = new AddLimitService(creditCardDatabase);
        repayService = new RepayService(creditCardDatabase);
        closeCycleService = new CloseCycleService(creditCardDatabase);
        ownershipService = new OwnershipService(ownershipDatabase);
    }

    [Fact]
    public void CanWithdraw()
    {
        var creditCard = NewCreditCard();
        addLimitService.AddLimit(creditCard, Money.Of(100, "USD"));
        ownershipService.AddAccess(creditCard, OSKAR);

        var result = withdrawService.Withdraw(creditCard, Money.Of(50, "USD"), OSKAR);

        Assert.Equal(Result.Success, result);
        Assert.Equal(Money.Of(50, "USD"), AvailableLimit(creditCard));
    }

    [Fact]
    public void CantWithdrawMoreThanLimit()
    {
        var creditCard = NewCreditCard();
        addLimitService.AddLimit(creditCard, Money.Of(100, "USD"));
        ownershipService.AddAccess(creditCard, OSKAR);

        var result = withdrawService.Withdraw(creditCard, Money.Of(500, "USD"), OSKAR);

        Assert.Equal(Result.Failure, result);
        Assert.Equal(Money.Of(100, "USD"), AvailableLimit(creditCard));
    }

    [Fact]
    public void CantWithdrawMoreThan45TimesInCycle()
    {
        var creditCard = NewCreditCard();
        addLimitService.AddLimit(creditCard, Money.Of(100, "USD"));
        ownershipService.AddAccess(creditCard, OSKAR);

        for (var i = 1; i <= 45; i++)
        {
            withdrawService.Withdraw(creditCard, Money.Of(1, "USD"), OSKAR);
        }

        var result = withdrawService.Withdraw(creditCard, Money.Of(1, "USD"), OSKAR);

        Assert.Equal(Result.Failure, result);
        Assert.Equal(Money.Of(55, "USD"), AvailableLimit(creditCard));
    }

    [Fact]
    public void CanRepay()
    {
        var creditCard = NewCreditCard();
        addLimitService.AddLimit(creditCard, Money.Of(100, "USD"));
        ownershipService.AddAccess(creditCard, OSKAR);
        withdrawService.Withdraw(creditCard, Money.Of(50, "USD"), OSKAR);

        var result = repayService.Repay(creditCard, Money.Of(40, "USD"));

        Assert.Equal(Result.Success, result);
        Assert.Equal(Money.Of(90, "USD"), AvailableLimit(creditCard));
    }

    [Fact]
    public void CanWithdrawInNextCycle()
    {
        var creditCard = NewCreditCard();
        addLimitService.AddLimit(creditCard, Money.Of(100, "USD"));
        ownershipService.AddAccess(creditCard, OSKAR);

        for (var i = 1; i <= 45; i++)
        {
            withdrawService.Withdraw(creditCard, Money.Of(1, "USD"), OSKAR);
        }

        closeCycleService.Close(creditCard);

        var result = withdrawService.Withdraw(creditCard, Money.Of(1, "USD"), OSKAR);

        Assert.Equal(Result.Success, result);
        Assert.Equal(Money.Of(54, "USD"), AvailableLimit(creditCard));
    }

    [Fact]
    public void CanWithdrawWhenNoAccess()
    {
        var creditCard = NewCreditCard();
        addLimitService.AddLimit(creditCard, Money.Of(100, "USD"));

        var result = withdrawService.Withdraw(creditCard, Money.Of(50, "USD"), KUBA);

        Assert.Equal(Result.Failure, result);
        Assert.Equal(Money.Of(100, "USD"), AvailableLimit(creditCard));
    }

    [Fact]
    public void CanAddAccess()
    {
        var creditCard = NewCreditCard();
        addLimitService.AddLimit(creditCard, Money.Of(100, "USD"));

        var accessResult = ownershipService.AddAccess(creditCard, KUBA);

        var withdrawResult = withdrawService.Withdraw(creditCard, Money.Of(50, "USD"), KUBA);
        Assert.Equal(Result.Success, accessResult);
        Assert.Equal(Result.Success, withdrawResult);
        Assert.Equal(Money.Of(50, "USD"), AvailableLimit(creditCard));
    }

    [Fact]
    public void CantAddMoreThan2Owners()
    {
        var creditCard = NewCreditCard();
        addLimitService.AddLimit(creditCard, Money.Of(100, "USD"));

        var firstAccess = ownershipService.AddAccess(creditCard, KUBA);
        var secondAccess = ownershipService.AddAccess(creditCard, OSKAR);

        var thirdAccess = ownershipService.AddAccess(creditCard, OwnerId.Random());

        Assert.Equal(Result.Success, firstAccess);
        Assert.Equal(Result.Success, secondAccess);
        Assert.Equal(Result.Failure, thirdAccess);
    }

    [Fact]
    public void CanRevokeAccess()
    {
        var creditCard = NewCreditCard();
        addLimitService.AddLimit(creditCard, Money.Of(100, "USD"));
        ownershipService.AddAccess(creditCard, KUBA);

        var withdrawResult = withdrawService.Withdraw(creditCard, Money.Of(50, "USD"), KUBA);

        var revoke = ownershipService.RevokeAccess(creditCard, KUBA);

        var secondWithdrawResult = withdrawService.Withdraw(creditCard, Money.Of(50, "USD"), KUBA);

        Assert.Equal(Result.Success, revoke);
        Assert.Equal(Result.Success, withdrawResult);
        Assert.Equal(Result.Failure, secondWithdrawResult);
    }

    private CardId NewCreditCard()
    {
        var virtualCreditCard = VirtualCreditCard.Create(CardId.Random());
        creditCardDatabase.Save(virtualCreditCard, 0);
        return virtualCreditCard.Id;
    }

    private Money AvailableLimit(CardId creditCard) =>
        creditCardDatabase.Find(creditCard).AvailableLimit;
}
