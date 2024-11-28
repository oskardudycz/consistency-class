namespace ConsistencyClass.Tests;

using static Result;
using static CurrencyUnit;

public class VirtualCreditCardTest
{
    private static readonly OwnerId OSKAR = OwnerId.Random();
    private static readonly OwnerId KUBA = OwnerId.Random();

    [Fact]
    public void CanWithdraw()
    {
        // given
        var creditCard = VirtualCreditCard.WithLimitAndOwner(Money.Of(100, USD), OSKAR);

        // when
        var result = creditCard.Withdraw(Money.Of(50, USD), OSKAR);

        // Then
        Assert.Equal(Success, result);
        Assert.Equal(Money.Of(50, USD), creditCard.AvailableLimit);
    }

    [Fact]
    public void CantWithdrawMoreThanLimit()
    {
        // given
        var creditCard = VirtualCreditCard.WithLimitAndOwner(Money.Of(100, USD), OSKAR);

        // when
        var result = creditCard.Withdraw(Money.Of(500, USD), OSKAR);

        // then
        Assert.Equal(Failure, result);
        Assert.Equal(Money.Of(100, USD), creditCard.AvailableLimit);
    }

    [Fact]
    public void CantWithdrawMoreThan45TimesInCycle()
    {
        // given
        var creditCard = VirtualCreditCard.WithLimitAndOwner(Money.Of(100, USD), OSKAR);
        // and
        for (var i = 1; i <= 45; i++)
        {
            creditCard.Withdraw(Money.Of(1, USD), OSKAR);
        }

        // when
        var result = creditCard.Withdraw(Money.Of(1, USD), OSKAR);

        //then
        Assert.Equal(Failure, result);
        Assert.Equal(Money.Of(55, USD), creditCard.AvailableLimit);
    }

    [Fact]
    public void CanRepay()
    {
        //given
        var creditCard = VirtualCreditCard.WithLimitAndOwner(Money.Of(100, USD), OSKAR);
        //and
        creditCard.Withdraw(Money.Of(50, USD), OSKAR);
        //when
        var result = creditCard.Repay(Money.Of(40, USD));
        //then
        Assert.Equal(Success, result);
        Assert.Equal(Money.Of(90, USD), creditCard.AvailableLimit);
    }

    [Fact]
    public void CanWithdrawInNextCycle()
    {
        // given
        var creditCard = VirtualCreditCard.WithLimitAndOwner(Money.Of(100, USD), OSKAR);
        // and
        for (var i = 1; i <= 45; i++)
        {
            creditCard.Withdraw(Money.Of(1, USD), OSKAR);
        }

        // and
        creditCard.CloseCycle();

        // when
        var result = creditCard.Withdraw(Money.Of(1, USD), OSKAR);

        // then
        Assert.Equal(Success, result);
        Assert.Equal(Money.Of(54, USD), creditCard.AvailableLimit);
    }

    [Fact]
    public void CanAddAccess()
    {
        //given
        var creditCard = VirtualCreditCard.WithLimitAndOwner(Money.Of(100, USD), OSKAR);
        //when
        var accessResult = creditCard.AddAccess(KUBA);
        //then
        var withdrawResult = creditCard.Withdraw(Money.Of(50, USD), KUBA);
        Assert.Equal(Success, accessResult);
        Assert.Equal(Success, withdrawResult);
        Assert.Equal(Money.Of(50, USD), creditCard.AvailableLimit);
    }

    [Fact]
    public void CantAddMoreThan2Owners()
    {
        //given
        var creditCard = VirtualCreditCard.WithLimitAndOwner(Money.Of(100, USD), OSKAR);
        //and
        var secondAccess = creditCard.AddAccess(KUBA);
        //when
        var thirdAccess = creditCard.AddAccess(OwnerId.Random());
        //then
        Assert.Equal(Success, secondAccess);
        Assert.Equal(Failure, thirdAccess);
    }

    [Fact]
    public void CantRevokeAccess()
    {
        //given
        var creditCard = VirtualCreditCard.WithLimitAndOwner(Money.Of(100, USD), OSKAR);
        //and
        var access = creditCard.AddAccess(KUBA);
        //and
        var withdrawResult = creditCard.Withdraw(Money.Of(50, USD), KUBA);
        //when
        var revoke = creditCard.RevokeAccess(KUBA);
        //then
        var secondWithdrawResult = creditCard.Withdraw(Money.Of(50, USD), KUBA);
        //then
        Assert.Equal(Success, revoke);
        Assert.Equal(Success, withdrawResult);
        Assert.Equal(Failure, secondWithdrawResult);
    }
}
