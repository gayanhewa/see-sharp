using Microsoft.EntityFrameworkCore;
using SeeSharp.Application.Abstractions;
using SeeSharp.Domain.Enums;

namespace SeeSharp.Application.Reports;

public static class ReportHandlers
{
    public static async Task<SummaryResponse> GetSummaryAsync(
        IAppDbContext db, DateOnly from, DateOnly to, CancellationToken ct)
    {
        var paidInvoices = await db.Invoices.AsNoTracking()
            .Where(i => i.Status == InvoiceStatus.Paid && i.IssueDate >= from && i.IssueDate <= to)
            .Include(i => i.LineItems)
            .ToListAsync(ct);

        var expenses = await db.Expenses.AsNoTracking()
            .Where(e => e.Date >= from && e.Date <= to)
            .ToListAsync(ct);

        var incomeByMonth = paidInvoices
            .GroupBy(i => (i.IssueDate.Year, i.IssueDate.Month))
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Total));

        var expenseByMonth = expenses
            .GroupBy(e => (e.Date.Year, e.Date.Month))
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount));

        var months = incomeByMonth.Keys.Union(expenseByMonth.Keys)
            .OrderBy(k => k.Item1).ThenBy(k => k.Item2)
            .Select(k =>
            {
                var income = incomeByMonth.GetValueOrDefault(k, 0m);
                var exp = expenseByMonth.GetValueOrDefault(k, 0m);
                return new MonthlySummaryRow(k.Item1, k.Item2, income, exp, income - exp);
            })
            .ToList();

        var totalIncome = months.Sum(m => m.Income);
        var totalExpenses = months.Sum(m => m.Expenses);

        return new SummaryResponse(from, to, totalIncome, totalExpenses, totalIncome - totalExpenses, months);
    }
}
