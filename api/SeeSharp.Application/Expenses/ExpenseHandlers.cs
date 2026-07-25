using Microsoft.EntityFrameworkCore;
using SeeSharp.Application.Abstractions;
using SeeSharp.Application.Common;
using SeeSharp.Domain.Entities;

namespace SeeSharp.Application.Expenses;

public static class ExpenseHandlers
{
    public static async Task<ExpenseResponse> CreateAsync(
        IAppDbContext db, CreateExpenseRequest req, CancellationToken ct)
    {
        var expense = Expense.Create(req.Description, req.Amount, req.Date, req.Vendor, req.CategoryId);
        db.Expenses.Add(expense);
        await db.SaveChangesAsync(ct);
        return ExpenseResponse.From(expense);
    }

    public static async Task<ExpenseResponse?> UpdateAsync(
        IAppDbContext db, Guid id, UpdateExpenseRequest req, CancellationToken ct)
    {
        var expense = await db.Expenses.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (expense is null) return null;
        expense.Update(req.Description, req.Amount, req.Date, req.Vendor, req.CategoryId);
        await db.SaveChangesAsync(ct);
        return ExpenseResponse.From(expense);
    }

    public static async Task<bool> DeleteAsync(IAppDbContext db, Guid id, CancellationToken ct)
    {
        var expense = await db.Expenses.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (expense is null) return false;
        db.Expenses.Remove(expense);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public static async Task<ExpenseResponse?> GetAsync(IAppDbContext db, Guid id, CancellationToken ct)
    {
        var expense = await db.Expenses.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct);
        return expense is null ? null : ExpenseResponse.From(expense);
    }

    public static async Task<PagedResult<ExpenseResponse>> ListAsync(
        IAppDbContext db, Guid? categoryId, DateOnly? from, DateOnly? to, int page, int pageSize, CancellationToken ct)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var query = db.Expenses.AsNoTracking().AsQueryable();
        if (categoryId is not null) query = query.Where(e => e.CategoryId == categoryId);
        if (from is not null) query = query.Where(e => e.Date >= from);
        if (to is not null) query = query.Where(e => e.Date <= to);

        query = query.OrderByDescending(e => e.Date);
        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(e => new ExpenseResponse(e.Id, e.CategoryId, e.Description, e.Amount, e.Date, e.Vendor, e.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<ExpenseResponse>(items, page, pageSize, total);
    }
}
