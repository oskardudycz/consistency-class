using System.Collections.Concurrent;

namespace ConsistencyClass.Tests;

using static Result;
using static CurrencyUnit;

public class WithdrawingTest
{
    private readonly EventStore eventStore = new();
    private readonly BillingCycleDatabase billingCycleDatabase;
    private readonly VirtualCreditCardDatabase creditCardDatabase;
    private readonly OwnershipDatabase ownershipDatabase = new();

    private readonly BillingCycleService billingCycleService;
    private readonly WithdrawService withdrawService;
    private readonly AddLimitService addLimitService;
    private readonly RepayService repayService;
    private readonly OwnershipService ownershipService;

    private readonly BillingCycleEventHandler eventHandler;

    private static readonly OwnerId OSKAR = OwnerId.Random();
    private static readonly OwnerId KUBA = OwnerId.Random();

    public WithdrawingTest()
    {
        billingCycleDatabase = new BillingCycleDatabase(eventStore);
        creditCardDatabase = new VirtualCreditCardDatabase(eventStore);

        billingCycleService = new BillingCycleService(creditCardDatabase, billingCycleDatabase);
        withdrawService = new WithdrawService(billingCycleDatabase, ownershipDatabase);
        addLimitService = new AddLimitService(creditCardDatabase);
        repayService = new RepayService(billingCycleDatabase);
        ownershipService = new OwnershipService(ownershipDatabase);

        eventHandler = new BillingCycleEventHandler(creditCardDatabase, billingCycleDatabase);

        eventStore.Subscribe(eventHandler.Handle);
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
        // and
        var cycleId = OpenBillingCycle(creditCard);

        // when
        var result = withdrawService.Withdraw(cycleId, Money.Of(50, USD), OSKAR);

        // Then
        Assert.Equal(Success, result);
        Assert.Equal(Money.Of(50, USD), AvailableLimit(cycleId));
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
        // and
        var cycleId = OpenBillingCycle(creditCard);

        var result = withdrawService.Withdraw(cycleId, Money.Of(500, USD), OSKAR);

        Assert.Equal(Failure, result);
        Assert.Equal(Money.Of(100, USD), AvailableLimit(cycleId));
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
        // and
        var cycleId = OpenBillingCycle(creditCard);
        // and
        for (var i = 1; i <= 45; i++)
        {
            withdrawService.Withdraw(cycleId, Money.Of(1, USD), OSKAR);
        }

        // when
        var result = withdrawService.Withdraw(cycleId, Money.Of(1, USD), OSKAR);

        //then
        Assert.Equal(Failure, result);
        Assert.Equal(Money.Of(55, USD), AvailableLimit(cycleId));
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
        var cycleId = OpenBillingCycle(creditCard);
        // and
        withdrawService.Withdraw(cycleId, Money.Of(50, USD), OSKAR);

        // when
        var result = repayService.Repay(cycleId, Money.Of(40, USD));

        // then
        Assert.Equal(Success, result);
        Assert.Equal(Money.Of(90, USD), AvailableLimit(cycleId));
    }

    [Fact]
    public void CanWithdrawInNextCycleIfWholeDebtWasPaid()
    {
        // given
        var creditCard = NewCreditCard();
        // and
        addLimitService.AddLimit(creditCard, Money.Of(100, USD));
        // and
        ownershipService.AddAccess(creditCard, OSKAR);
        // and
        var initialCycleId = OpenBillingCycle(creditCard);
        // and
        withdrawService.Withdraw(initialCycleId, Money.Of(100, USD), OSKAR);
        // and
        repayService.Repay(initialCycleId, Money.Of(100, USD));
        // and
        billingCycleService.Close(initialCycleId);
        // and
        var cycleId = OpenBillingCycle(creditCard);

        // when
        var result = withdrawService.Withdraw(cycleId, Money.Of(1, USD), OSKAR);

        // then
        Assert.Equal(Success, result);
        Assert.Equal(Money.Of(99, USD), AvailableLimit(cycleId));
    }

    [Fact]
    public void CanWithdrawWhenNoAccess()
    {
        // given
        var creditCard = NewCreditCard();
        // and
        addLimitService.AddLimit(creditCard, Money.Of(100, USD));
        // and
        var cycleId = OpenBillingCycle(creditCard);

        // when
        var result = withdrawService.Withdraw(cycleId, Money.Of(50, USD), KUBA);

        // then
        Assert.Equal(Failure, result);
        Assert.Equal(Money.Of(100, USD), AvailableLimit(cycleId));
    }

    [Fact]
    public void CanAddAccess()
    {
        // given
        var creditCard = NewCreditCard();
        // and
        addLimitService.AddLimit(creditCard, Money.Of(100, USD));
        // and
        var cycleId = OpenBillingCycle(creditCard);

        var accessResult = ownershipService.AddAccess(creditCard, KUBA);

        var withdrawResult = withdrawService.Withdraw(cycleId, Money.Of(50, USD), KUBA);
        Assert.Equal(Success, accessResult);
        Assert.Equal(Success, withdrawResult);
        Assert.Equal(Money.Of(50, USD), AvailableLimit(cycleId));
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
        var secondAccess = ownershipService.AddAccess(creditCard, OSKAR);

        // when
        var thirdAccess = ownershipService.AddAccess(creditCard, OwnerId.Random());

        // then
        Assert.Equal(Success, firstAccess);
        Assert.Equal(Success, secondAccess);
        Assert.Equal(Failure, thirdAccess);
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
        var cycleId = OpenBillingCycle(creditCard);
        // and
        var withdrawResult = withdrawService.Withdraw(cycleId, Money.Of(50, USD), KUBA);

        // when
        var revoke = ownershipService.RevokeAccess(creditCard, KUBA);

        // then
        var secondWithdrawResult = withdrawService.Withdraw(cycleId, Money.Of(50, USD), KUBA);

        Assert.Equal(Success, revoke);
        Assert.Equal(Success, withdrawResult);
        Assert.Equal(Failure, secondWithdrawResult);
    }

    [Fact]
    public async Task CantWithdrawConcurrently()
    {
        // given
        var creditCard = NewCreditCard();
        // and
        addLimitService.AddLimit(creditCard, Money.Of(100, USD));
        // and
        ownershipService.AddAccess(creditCard, OSKAR);
        // and
        var cycleId = OpenBillingCycle(creditCard);

        var results = new ConcurrentBag<Result>();
        var tasks = Enumerable.Range(0, 44).Select(_ => Task.Run(async () =>
        {
            await Task.Delay(100);
            results.Add(withdrawService.Withdraw(cycleId, Money.Of(1, USD), OSKAR));
        }));

        await Task.WhenAll(tasks);

        Assert.Contains(Failure, results);
        Assert.Contains(Success, results);
        Assert.True(AvailableLimit(cycleId).Amount < 100);
    }

    private CardId NewCreditCard()
    {
        var virtualCreditCard = VirtualCreditCard.Create(CardId.Random(), USD);
        creditCardDatabase.Save(virtualCreditCard, 0);
        return virtualCreditCard.Id;
    }

    private Money AvailableLimit(BillingCycleId cycleId) =>
        billingCycleDatabase.Find(cycleId).AvailableLimit;


    private BillingCycleId OpenBillingCycle(CardId creditCard)
    {
        var result = billingCycleService.OpenNextCycle(creditCard);

        Assert.Equal(Success, result);

        var cycleId = billingCycleService.GetCurrentlyOpenedBillingCycleId(creditCard);

        Assert.NotNull(cycleId);

        return cycleId;
    }
}
