namespace SeeSharp.Domain.ValueObjects;

public readonly record struct Money
{
    public decimal Amount { get; }

    private Money(decimal amount) => Amount = amount;

    public static Money Zero => new(0m);

    public static Money From(decimal amount)
    {
        if (amount < 0m)
            throw new ArgumentOutOfRangeException(nameof(amount), "Money cannot be negative.");
        return new Money(amount);
    }

    public static Money operator +(Money left, Money right) => new(left.Amount + right.Amount);
}
