using SeeSharp.Domain.Entities;

namespace SeeSharp.Application.Expenses;

public record CreateExpenseRequest(string Description, decimal Amount, DateOnly Date, string? Vendor, Guid? CategoryId);

public record UpdateExpenseRequest(string Description, decimal Amount, DateOnly Date, string? Vendor, Guid? CategoryId);

public record ExpenseResponse(
    Guid Id, Guid? CategoryId, string Description, decimal Amount, DateOnly Date, string? Vendor, DateTimeOffset CreatedAt)
{
    public static ExpenseResponse From(Expense expense) =>
        new(expense.Id, expense.CategoryId, expense.Description, expense.Amount, expense.Date, expense.Vendor, expense.CreatedAt);
}
