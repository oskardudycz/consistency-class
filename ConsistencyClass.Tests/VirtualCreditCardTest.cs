namespace ConsistencyClass.Tests;

using static Result;
using static CurrencyUnit;

public class VirtualCreditCardTest
{
    [Fact]
    public void CanWithdraw()
    {
        // given
        var creditCard = VirtualCreditCard.WithLimit(Money.Of(100, USD));

        // when
        var result = creditCard.Withdraw(Money.Of(50, USD));

        // Then
        Assert.Equal(Success, result);
        Assert.Equal(Money.Of(50, USD), creditCard.AvailableLimit);
    }

    [Fact]
    public void CantWithdrawMoreThanLimit()
    {
        // given
        var creditCard = VirtualCreditCard.WithLimit(Money.Of(100, USD));

        // when
        var result = creditCard.Withdraw(Money.Of(500, USD));

        // then
        Assert.Equal(Failure, result);
        Assert.Equal(Money.Of(100, USD), creditCard.AvailableLimit);
    }

    [Fact]
    public void CantWithdrawMoreThan45TimesInCycle()
    {
        // given
        var creditCard = VirtualCreditCard.WithLimit(Money.Of(100, USD));
        // and
        for (var i = 1; i <= 45; i++)
        {
            creditCard.Withdraw(Money.Of(1, USD));
        }

        // when
        var result = creditCard.Withdraw(Money.Of(1, USD));

        //then
        Assert.Equal(Failure, result);
        Assert.Equal(Money.Of(55, USD), creditCard.AvailableLimit);
    }

    [Fact]
    public void CanRepay()
    {
        //given
        var creditCard = VirtualCreditCard.WithLimit(Money.Of(100, USD));
        //and
        creditCard.Withdraw(Money.Of(50, USD));
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
        var creditCard = VirtualCreditCard.WithLimit(Money.Of(100, USD));
        // and
        for (var i = 1; i <= 45; i++)
        {
            creditCard.Withdraw(Money.Of(1, USD));
        }

        // and
        creditCard.CloseCycle();

        // when
        var result = creditCard.Withdraw(Money.Of(1, USD));

        // then
        Assert.Equal(Success, result);
        Assert.Equal(Money.Of(54, USD), creditCard.AvailableLimit);
    }
}
