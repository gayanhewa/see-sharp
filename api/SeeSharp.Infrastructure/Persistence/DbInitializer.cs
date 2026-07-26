using Microsoft.EntityFrameworkCore;
using SeeSharp.Domain.Entities;

namespace SeeSharp.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task InitializeAsync(AppDbContext db, CancellationToken ct = default)
    {
        await db.Database.MigrateAsync(ct);

        if (await db.Clients.AnyAsync(ct)) return;

        var acme = Client.Create("Acme Co", "billing@acme.test", "1 Acme Way");
        var globex = Client.Create("Globex", "ap@globex.test", null);
        db.Clients.AddRange(acme, globex);

        var software = Category.Create("Software");
        var travel = Category.Create("Travel");
        db.Categories.AddRange(software, travel);

        var invoice = Invoice.Create(acme.Id, "INV-1001",
            new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30), "Thanks for your business.");
        invoice.AddLineItem("Consulting", 10, 150m);
        invoice.AddLineItem("Setup fee", 1, 500m);
        invoice.MarkAsSent();
        invoice.MarkAsPaid();
        db.Invoices.Add(invoice);

        db.Expenses.Add(Expense.Create("JetBrains license", 199m, new DateOnly(2026, 6, 5), "JetBrains", software.Id));
        db.Expenses.Add(Expense.Create("Client visit", 85.50m, new DateOnly(2026, 6, 12), "Uber", travel.Id));

        await db.SaveChangesAsync(ct);
    }
}
