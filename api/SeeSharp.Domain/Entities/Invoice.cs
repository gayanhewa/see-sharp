using SeeSharp.Domain.Enums;
using SeeSharp.Domain.Exceptions;

namespace SeeSharp.Domain.Entities;

public sealed class Invoice
{
    private readonly List<InvoiceLineItem> _lineItems = [];

    public Guid Id { get; private set; }
    public Guid ClientId { get; private set; }
    public string Number { get; private set; } = default!;
    public InvoiceStatus Status { get; private set; }
    public DateOnly IssueDate { get; private set; }
    public DateOnly DueDate { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyCollection<InvoiceLineItem> LineItems => _lineItems.AsReadOnly();
    public decimal Total => _lineItems.Sum(item => item.LineTotal);

    private Invoice() { }

    public static Invoice Create(Guid clientId, string number, DateOnly issueDate, DateOnly dueDate, string? notes)
    {
        if (clientId == Guid.Empty)
            throw new ArgumentException("ClientId is required.", nameof(clientId));
        if (string.IsNullOrWhiteSpace(number))
            throw new ArgumentException("Invoice number is required.", nameof(number));
        if (dueDate < issueDate)
            throw new ArgumentException("Due date cannot be before issue date.", nameof(dueDate));

        return new Invoice
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            Number = number.Trim(),
            Status = InvoiceStatus.Draft,
            IssueDate = issueDate,
            DueDate = dueDate,
            Notes = notes?.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void AddLineItem(string description, int quantity, decimal unitPrice)
    {
        if (Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Line items can only be added while the invoice is a draft.");
        _lineItems.Add(InvoiceLineItem.Create(Id, description, quantity, unitPrice));
    }

    public void ClearLineItems()
    {
        if (Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Line items can only be changed while the invoice is a draft.");
        _lineItems.Clear();
    }

    public void MarkAsSent()
    {
        if (Status != InvoiceStatus.Draft)
            throw new InvalidInvoiceTransitionException(Status, InvoiceStatus.Sent);
        Status = InvoiceStatus.Sent;
    }

    public void MarkAsPaid()
    {
        if (Status is not (InvoiceStatus.Sent or InvoiceStatus.Overdue))
            throw new InvalidInvoiceTransitionException(Status, InvoiceStatus.Paid);
        Status = InvoiceStatus.Paid;
    }

    public void MarkAsOverdue()
    {
        if (Status != InvoiceStatus.Sent)
            throw new InvalidInvoiceTransitionException(Status, InvoiceStatus.Overdue);
        Status = InvoiceStatus.Overdue;
    }

    public void Cancel()
    {
        if (Status is not (InvoiceStatus.Draft or InvoiceStatus.Sent))
            throw new InvalidInvoiceTransitionException(Status, InvoiceStatus.Cancelled);
        Status = InvoiceStatus.Cancelled;
    }
}
