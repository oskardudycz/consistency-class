namespace ConsistencyClass;

internal record CurrencyUnit: IComparable<CurrencyUnit>
{
    public static readonly CurrencyUnit Unset = new("Unset");
    public static readonly CurrencyUnit USD = OfCode("USD");
    public static readonly CurrencyUnit EUR = OfCode("EUR");
    public string Code { get; }

    public static CurrencyUnit OfCode(string code) => !string.IsNullOrWhiteSpace(code)
        ? new CurrencyUnit(code)
        : throw new ArgumentException($"Currency {nameof(code)} cannot be null or empty.", nameof(code));

    private CurrencyUnit(string code) =>
        Code = code.ToUpperInvariant();

    public override string ToString() => Code;

    public int CompareTo(CurrencyUnit? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (other is null) return 1;
        return string.Compare(Code, other.Code, StringComparison.Ordinal);
    }
}

internal record Money: IComparable<Money>
{
    public static Money Unset = new(0, CurrencyUnit.Unset);
    public static Money Zero(CurrencyUnit currency) => new(0, currency);

    public decimal Amount { get; }
    public CurrencyUnit Currency { get; }

    private Money(decimal amount, CurrencyUnit currency)
    {
        Currency = currency;
        Amount = amount;
    }

    public static Money Of(decimal amount, CurrencyUnit currency)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be negative.");

        if (currency == CurrencyUnit.Unset)
            throw new ArgumentException("Currency cannot be unset.", nameof(currency));

        return new Money(amount, currency);
    }

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

    public bool IsZero => Amount == 0;

    private void EnsureSameCurrency(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Cannot operate on Money with different currencies.");
    }

    public override string ToString() => $"{Amount:0.00} {Currency}";

    public int CompareTo(Money? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (other is null) return 1;
        var amountComparison = Amount.CompareTo(other.Amount);
        if (amountComparison != 0) return amountComparison;
        return Currency.CompareTo(other.Currency);
    }
}
