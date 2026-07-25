using Microsoft.EntityFrameworkCore;
using SeeSharp.Domain.Entities;

namespace SeeSharp.Application.Abstractions;

public interface IAppDbContext
{
    DbSet<Client> Clients { get; }
    DbSet<Invoice> Invoices { get; }
    DbSet<InvoiceLineItem> InvoiceLineItems { get; }
    DbSet<Expense> Expenses { get; }
    DbSet<Category> Categories { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
