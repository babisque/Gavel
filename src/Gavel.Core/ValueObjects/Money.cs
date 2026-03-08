namespace Gavel.Domain.ValueObjects;

public record Money
{
    public decimal Amount { get; init; }
    public string Currency { get; init; }

    public Money() { }

    public Money(decimal amount, string currency = "USD")
    {
        if (amount < 0) 
            throw new ArgumentException("Amount cannot be negative.", nameof(amount));

        if (string.IsNullOrEmpty(currency))
            throw new ArgumentException("Currency cannot be null or empty.", nameof(currency));

        Amount = amount;
        Currency = currency;
    }

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Cannot add money with different currencies.");

        return new Money(Amount + other.Amount, Currency);
    }

    public bool IsGreaterThan(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Cannot compare money with different currencies.");

        return Amount > other.Amount;
    }
}
