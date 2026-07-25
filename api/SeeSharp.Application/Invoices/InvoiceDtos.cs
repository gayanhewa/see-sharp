using SeeSharp.Domain.Entities;

namespace SeeSharp.Application.Invoices;

public record LineItemRequest(string Description, int Quantity, decimal UnitPrice);

public record LineItemResponse(Guid Id, string Description, int Quantity, decimal UnitPrice, decimal LineTotal);

public record CreateInvoiceRequest(
    Guid ClientId, string Number, DateOnly IssueDate, DateOnly DueDate, string? Notes,
    IReadOnlyList<LineItemRequest> LineItems);

public record UpdateInvoiceRequest(
    string Number, DateOnly IssueDate, DateOnly DueDate, string? Notes,
    IReadOnlyList<LineItemRequest> LineItems);

public record ChangeStatusRequest(string Status);

public record InvoiceResponse(
    Guid Id, Guid ClientId, string Number, string Status,
    DateOnly IssueDate, DateOnly DueDate, string? Notes, decimal Total, DateTimeOffset CreatedAt,
    IReadOnlyList<LineItemResponse> LineItems)
{
    public static InvoiceResponse From(Invoice invoice) => new(
        invoice.Id, invoice.ClientId, invoice.Number, invoice.Status.ToString(),
        invoice.IssueDate, invoice.DueDate, invoice.Notes, invoice.Total, invoice.CreatedAt,
        invoice.LineItems
            .Select(li => new LineItemResponse(li.Id, li.Description, li.Quantity, li.UnitPrice, li.LineTotal))
            .ToList());
}
