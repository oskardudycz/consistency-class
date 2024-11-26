namespace ConsistencyClass;

public class Money: IEquatable<Money>, IComparable<Money>
{
    public decimal Amount { get; }
    public string Currency { get; }
    private Money(decimal amount, string currency)
    {
        if (Amount < 0)
            throw new ArgumentOutOfRangeException(nameof(Amount), "Amount cannot be negative.");

        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("currency cannot be null or empty.", nameof(Currency));

        Currency = currency.ToUpperInvariant();
        Amount = amount;
    }

    public static Money Of(decimal amount, string currency) => new(amount, currency);

    public static Money Unset = new(0, "Unset");
    public static Money Zero(string currency) => new(0, currency);

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount - other.Amount, Currency);
    }

    public bool IsLessThan(Money other)
    {
        EnsureSameCurrency(other);
        return Amount < other.Amount;
    }

    public bool IsPositiveOrZero => Amount >= 0;

    private void EnsureSameCurrency(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Cannot operate on Money with different currencies.");
    }

    public override string ToString() => $"{Amount:0.00} {Currency}";

    public bool Equals(Money? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Amount == other.Amount && Currency == other.Currency;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((Money)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Amount, Currency);
    }

    public int CompareTo(Money? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (other is null) return 1;
        var amountComparison = Amount.CompareTo(other.Amount);
        if (amountComparison != 0) return amountComparison;
        return string.Compare(Currency, other.Currency, StringComparison.Ordinal);
    }
}

