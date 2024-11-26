namespace ConsistencyClass.Tests;

using static CurrencyUnit;

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
        // given
        var creditCard = NewCreditCard();
        // and
        addLimitService.AddLimit(creditCard, Money.Of(100, USD));
        // and
        ownershipService.AddAccess(creditCard, OSKAR);

        // when
        var result = withdrawService.Withdraw(creditCard, Money.Of(50, USD), OSKAR);

        // then
        Assert.Equal(Result.Success, result);
        Assert.Equal(Money.Of(50, USD), AvailableLimit(creditCard));
    }

    [Fact]
    public void CantWithdrawMoreThanLimit()
    {
        // given
        var creditCard = NewCreditCard();
        // and
        addLimitService.AddLimit(creditCard, Money.Of(100, USD));
        // and
        ownershipService.AddAccess(creditCard, OSKAR);

        // when
        var result = withdrawService.Withdraw(creditCard, Money.Of(500, USD), OSKAR);

        // then
        Assert.Equal(Result.Failure, result);
        Assert.Equal(Money.Of(100, USD), AvailableLimit(creditCard));
    }

    [Fact]
    public void CantWithdrawMoreThan45TimesInCycle()
    {
        // given
        var creditCard = NewCreditCard();
        // and
        addLimitService.AddLimit(creditCard, Money.Of(100, USD));
        // and
        ownershipService.AddAccess(creditCard, OSKAR);

        for (var i = 1; i <= 45; i++)
        {
            withdrawService.Withdraw(creditCard, Money.Of(1, USD), OSKAR);
        }

        // when
        var result = withdrawService.Withdraw(creditCard, Money.Of(1, USD), OSKAR);

        // then
        Assert.Equal(Result.Failure, result);
        Assert.Equal(Money.Of(55, USD), AvailableLimit(creditCard));
    }

    [Fact]
    public void CanRepay()
    {
        // given
        var creditCard = NewCreditCard();
        // and
        addLimitService.AddLimit(creditCard, Money.Of(100, USD));
        // and
        ownershipService.AddAccess(creditCard, OSKAR);
        // and
        withdrawService.Withdraw(creditCard, Money.Of(50, USD), OSKAR);

        // when
        var result = repayService.Repay(creditCard, Money.Of(40, USD));

        // then
        Assert.Equal(Result.Success, result);
        Assert.Equal(Money.Of(90, USD), AvailableLimit(creditCard));
    }

    [Fact]
    public void CanWithdrawInNextCycle()
    {
        // given
        var creditCard = NewCreditCard();
        // and
        addLimitService.AddLimit(creditCard, Money.Of(100, USD));
        // and
        ownershipService.AddAccess(creditCard, OSKAR);
        // and
        for (var i = 1; i <= 45; i++)
        {
            withdrawService.Withdraw(creditCard, Money.Of(1, USD), OSKAR);
        }
        // and
        closeCycleService.Close(creditCard);

        // when
        var result = withdrawService.Withdraw(creditCard, Money.Of(1, USD), OSKAR);

        // then
        Assert.Equal(Result.Success, result);
        Assert.Equal(Money.Of(54, USD), AvailableLimit(creditCard));
    }

    [Fact]
    public void CanWithdrawWhenNoAccess()
    {
        // given
        var creditCard = NewCreditCard();
        // and
        addLimitService.AddLimit(creditCard, Money.Of(100, USD));

        // when
        var result = withdrawService.Withdraw(creditCard, Money.Of(50, USD), KUBA);

        // then
        Assert.Equal(Result.Failure, result);
        Assert.Equal(Money.Of(100, USD), AvailableLimit(creditCard));
    }

    [Fact]
    public void CanAddAccess()
    {
        // given
        var creditCard = NewCreditCard();
        // and
        addLimitService.AddLimit(creditCard, Money.Of(100, USD));

        // when
        var accessResult = ownershipService.AddAccess(creditCard, KUBA);

        // then
        var withdrawResult = withdrawService.Withdraw(creditCard, Money.Of(50, USD), KUBA);
        Assert.Equal(Result.Success, accessResult);
        Assert.Equal(Result.Success, withdrawResult);
        Assert.Equal(Money.Of(50, USD), AvailableLimit(creditCard));
    }

    [Fact]
    public void CantAddMoreThan2Owners()
    {
        // given
        var creditCard = NewCreditCard();
        // and
        addLimitService.AddLimit(creditCard, Money.Of(100, USD));
        // and
        var firstAccess = ownershipService.AddAccess(creditCard, KUBA);
        // and
        var secondAccess = ownershipService.AddAccess(creditCard, OSKAR);

        // when
        var thirdAccess = ownershipService.AddAccess(creditCard, OwnerId.Random());

        // then
        Assert.Equal(Result.Success, firstAccess);
        Assert.Equal(Result.Success, secondAccess);
        Assert.Equal(Result.Failure, thirdAccess);
    }

    [Fact]
    public void CanRevokeAccess()
    {
        // given
        var creditCard = NewCreditCard();
        // and
        addLimitService.AddLimit(creditCard, Money.Of(100, USD));
        // and
        ownershipService.AddAccess(creditCard, KUBA);
        // and
        var withdrawResult = withdrawService.Withdraw(creditCard, Money.Of(50, USD), KUBA);

        // when
        var revoke = ownershipService.RevokeAccess(creditCard, KUBA);

        // then
        var secondWithdrawResult = withdrawService.Withdraw(creditCard, Money.Of(50, USD), KUBA);

        Assert.Equal(Result.Success, revoke);
        Assert.Equal(Result.Success, withdrawResult);
        Assert.Equal(Result.Failure, secondWithdrawResult);
    }

    private CardId NewCreditCard()
    {
        var virtualCreditCard = VirtualCreditCard.Create(CardId.Random());
        creditCardDatabase.Save(virtualCreditCard);
        return virtualCreditCard.Id;
    }

    private Money AvailableLimit(CardId creditCard) =>
        creditCardDatabase.Find(creditCard).AvailableLimit;
}
