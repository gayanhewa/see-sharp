using SeeSharp.Domain.ValueObjects;

namespace SeeSharp.Domain.Entities;

public sealed class Expense
{
    public Guid Id { get; private set; }
    public Guid? CategoryId { get; private set; }
    public string Description { get; private set; } = default!;
    public decimal Amount { get; private set; }
    public DateOnly Date { get; private set; }
    public string? Vendor { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private Expense() { }

    public static Expense Create(string description, decimal amount, DateOnly date, string? vendor, Guid? categoryId)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Expense description is required.", nameof(description));

        return new Expense
        {
            Id = Guid.NewGuid(),
            Description = description.Trim(),
            Amount = Money.From(amount).Amount,
            Date = date,
            Vendor = vendor?.Trim(),
            CategoryId = categoryId,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Update(string description, decimal amount, DateOnly date, string? vendor, Guid? categoryId)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Expense description is required.", nameof(description));
        Description = description.Trim();
        Amount = Money.From(amount).Amount;
        Date = date;
        Vendor = vendor?.Trim();
        CategoryId = categoryId;
    }
}
