namespace ConsistencyClass.Tests;

using Xunit;
using static CurrencyUnit;
using static Result;

public class BillingCycleTest
{
    [Fact]
    public void CanWithdraw()
    {
        // Given
        var creditCard = BillingCycle.WithLimit(Money.Of(100, USD));

        // When
        var result = creditCard.Withdraw(Money.Of(50, USD));

        // Then
        Assert.Equal(Success, result);
        Assert.Equal(Money.Of(50, USD), creditCard.AvailableLimit);
    }

    [Fact]
    public void CantWithdrawMoreThanLimit()
    {
        // Given
        var creditCard = BillingCycle.WithLimit(Money.Of(100, USD));

        // When
        var result = creditCard.Withdraw(Money.Of(500, USD));

        // Then
        Assert.Equal(Failure, result);
        Assert.Equal(Money.Of(100, USD), creditCard.AvailableLimit);
    }

    [Fact]
    public void CantWithdrawMoreThan45TimesInCycle()
    {
        // Given
        var creditCard = BillingCycle.WithLimit(Money.Of(100, USD));

        // And
        for (var i = 1; i <= 45; i++)
        {
            creditCard.Withdraw(Money.Of(1, USD));
        }

        // When
        var result = creditCard.Withdraw(Money.Of(1, USD));

        // Then
        Assert.Equal(Failure, result);
        Assert.Equal(Money.Of(55, USD), creditCard.AvailableLimit);
    }

    [Fact]
    public void CanRepay()
    {
        // Given
        var creditCard = BillingCycle.WithLimit(Money.Of(100, USD));

        // And
        creditCard.Withdraw(Money.Of(50, USD));

        // When
        var result = creditCard.Repay(Money.Of(40, USD));

        // Then
        Assert.Equal(Success, result);
        Assert.Equal(Money.Of(90, USD), creditCard.AvailableLimit);
    }

    [Fact]
    public void CannotWithdrawInClosedCycle()
    {
        // Given
        var creditCard = BillingCycle.WithLimit(Money.Of(100, USD));

        // And
        for (var i = 1; i <= 45; i++)
        {
            creditCard.Withdraw(Money.Of(1, USD));
        }

        // And
        creditCard.CloseCycle();

        // When
        var result = creditCard.Withdraw(Money.Of(1, USD));

        // Then
        Assert.Equal(Failure, result);
    }
}

