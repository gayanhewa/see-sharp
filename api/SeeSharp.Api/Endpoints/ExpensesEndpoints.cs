using FluentValidation;
using SeeSharp.Application.Abstractions;
using SeeSharp.Application.Expenses;

namespace SeeSharp.Api.Endpoints;

public static class ExpensesEndpoints
{
    public static WebApplication MapExpenses(this WebApplication app)
    {
        var group = app.MapGroup("/expenses").WithTags("Expenses");

        group.MapGet("/", async (
            IAppDbContext db, CancellationToken ct, Guid? categoryId = null, DateOnly? from = null, DateOnly? to = null,
            int page = 1, int pageSize = 20)
            => Results.Ok(await ExpenseHandlers.ListAsync(db, categoryId, from, to, page, pageSize, ct)));

        group.MapGet("/{id:guid}", async (IAppDbContext db, Guid id, CancellationToken ct) =>
        {
            var expense = await ExpenseHandlers.GetAsync(db, id, ct);
            return expense is null ? Results.NotFound() : Results.Ok(expense);
        });

        group.MapPost("/", async (
            IAppDbContext db, IValidator<CreateExpenseRequest> validator, CreateExpenseRequest req, CancellationToken ct) =>
        {
            await validator.ValidateAndThrowAsync(req, ct);
            var created = await ExpenseHandlers.CreateAsync(db, req, ct);
            return Results.Created($"/expenses/{created.Id}", created);
        });

        group.MapPut("/{id:guid}", async (
            IAppDbContext db, IValidator<UpdateExpenseRequest> validator, Guid id, UpdateExpenseRequest req, CancellationToken ct) =>
        {
            await validator.ValidateAndThrowAsync(req, ct);
            var updated = await ExpenseHandlers.UpdateAsync(db, id, req, ct);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        });

        group.MapDelete("/{id:guid}", async (IAppDbContext db, Guid id, CancellationToken ct) =>
            await ExpenseHandlers.DeleteAsync(db, id, ct) ? Results.NoContent() : Results.NotFound());

        return app;
    }
}
