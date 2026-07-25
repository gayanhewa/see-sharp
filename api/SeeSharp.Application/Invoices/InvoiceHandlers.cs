using Microsoft.EntityFrameworkCore;
using SeeSharp.Application.Abstractions;
using SeeSharp.Application.Common;
using SeeSharp.Domain.Entities;
using SeeSharp.Domain.Enums;

namespace SeeSharp.Application.Invoices;

public static class InvoiceHandlers
{
    public static async Task<InvoiceResponse> CreateAsync(
        IAppDbContext db, CreateInvoiceRequest req, CancellationToken ct)
    {
        var invoice = Invoice.Create(req.ClientId, req.Number, req.IssueDate, req.DueDate, req.Notes);
        foreach (var li in req.LineItems)
            invoice.AddLineItem(li.Description, li.Quantity, li.UnitPrice);

        db.Invoices.Add(invoice);
        await db.SaveChangesAsync(ct);
        return InvoiceResponse.From(invoice);
    }

    public static async Task<InvoiceResponse?> UpdateAsync(
        IAppDbContext db, Guid id, UpdateInvoiceRequest req, CancellationToken ct)
    {
        var invoice = await db.Invoices
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == id, ct);
        if (invoice is null) return null;

        invoice.UpdateDetails(req.Number, req.IssueDate, req.DueDate, req.Notes);
        invoice.ClearLineItems();
        foreach (var li in req.LineItems)
            invoice.AddLineItem(li.Description, li.Quantity, li.UnitPrice);

        await db.SaveChangesAsync(ct);
        return InvoiceResponse.From(invoice);
    }

    public static async Task<bool> DeleteAsync(IAppDbContext db, Guid id, CancellationToken ct)
    {
        var invoice = await db.Invoices.FirstOrDefaultAsync(i => i.Id == id, ct);
        if (invoice is null) return false;
        db.Invoices.Remove(invoice);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public static async Task<InvoiceResponse?> GetAsync(IAppDbContext db, Guid id, CancellationToken ct)
    {
        var invoice = await db.Invoices.AsNoTracking()
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == id, ct);
        return invoice is null ? null : InvoiceResponse.From(invoice);
    }

    public static async Task<PagedResult<InvoiceResponse>> ListAsync(
        IAppDbContext db, InvoiceStatus? status, Guid? clientId, int page, int pageSize, CancellationToken ct)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var query = db.Invoices.AsNoTracking().Include(i => i.LineItems).AsQueryable();
        if (status is not null) query = query.Where(i => i.Status == status);
        if (clientId is not null) query = query.Where(i => i.ClientId == clientId);

        query = query.OrderByDescending(i => i.IssueDate);
        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return new PagedResult<InvoiceResponse>(
            items.Select(InvoiceResponse.From).ToList(), page, pageSize, total);
    }

    public static async Task<InvoiceResponse?> ChangeStatusAsync(
        IAppDbContext db, Guid id, string status, CancellationToken ct)
    {
        var invoice = await db.Invoices.Include(i => i.LineItems).FirstOrDefaultAsync(i => i.Id == id, ct);
        if (invoice is null) return null;

        switch (status.Trim().ToLowerInvariant())
        {
            case "sent": invoice.MarkAsSent(); break;
            case "paid": invoice.MarkAsPaid(); break;
            case "overdue": invoice.MarkAsOverdue(); break;
            case "cancelled": invoice.Cancel(); break;
            default: throw new ArgumentException($"Unknown status '{status}'.", nameof(status));
        }

        await db.SaveChangesAsync(ct);
        return InvoiceResponse.From(invoice);
    }
}
