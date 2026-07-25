namespace SeeSharp.Domain.Entities;

public sealed class InvoiceLineItem
{
    public Guid Id { get; private set; }
    public Guid InvoiceId { get; private set; }
    public string Description { get; private set; } = default!;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }

    public decimal LineTotal => Quantity * UnitPrice;

    private InvoiceLineItem() { }

    internal static InvoiceLineItem Create(Guid invoiceId, string description, int quantity, decimal unitPrice)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Line item description is required.", nameof(description));
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        if (unitPrice < 0m)
            throw new ArgumentOutOfRangeException(nameof(unitPrice), "Unit price cannot be negative.");

        return new InvoiceLineItem
        {
            Id = Guid.NewGuid(),
            InvoiceId = invoiceId,
            Description = description.Trim(),
            Quantity = quantity,
            UnitPrice = unitPrice
        };
    }
}
