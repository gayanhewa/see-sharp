using SeeSharp.Application.Abstractions;
using SeeSharp.Application.Categories;

namespace SeeSharp.Api.Endpoints;

public static class CategoriesEndpoints
{
    public static WebApplication MapCategories(this WebApplication app)
    {
        var group = app.MapGroup("/categories").WithTags("Categories");

        group.MapGet("/", async (IAppDbContext db, CancellationToken ct)
            => Results.Ok(await CategoryHandlers.ListAsync(db, ct)));

        group.MapPost("/", async (IAppDbContext db, CreateCategoryRequest req, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(req.Name))
                return Results.BadRequest(new { error = "Name is required." });
            var created = await CategoryHandlers.CreateAsync(db, req, ct);
            return Results.Created($"/categories/{created.Id}", created);
        });

        group.MapDelete("/{id:guid}", async (IAppDbContext db, Guid id, CancellationToken ct) =>
            await CategoryHandlers.DeleteAsync(db, id, ct) ? Results.NoContent() : Results.NotFound());

        return app;
    }
}
