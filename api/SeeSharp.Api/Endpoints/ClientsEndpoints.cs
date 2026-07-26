using FluentValidation;
using SeeSharp.Application.Abstractions;
using SeeSharp.Application.Clients;

namespace SeeSharp.Api.Endpoints;

public static class ClientsEndpoints
{
    public static WebApplication MapClients(this WebApplication app)
    {
        var group = app.MapGroup("/clients").WithTags("Clients");

        group.MapGet("/", async (IAppDbContext db, CancellationToken ct, int page = 1, int pageSize = 20)
            => Results.Ok(await ClientHandlers.ListAsync(db, page, pageSize, ct)));

        group.MapGet("/{id:guid}", async (IAppDbContext db, Guid id, CancellationToken ct) =>
        {
            var client = await ClientHandlers.GetAsync(db, id, ct);
            return client is null ? Results.NotFound() : Results.Ok(client);
        });

        group.MapPost("/", async (
            IAppDbContext db, IValidator<CreateClientRequest> validator, CreateClientRequest req, CancellationToken ct) =>
        {
            await validator.ValidateAndThrowAsync(req, ct);
            var created = await ClientHandlers.CreateAsync(db, req, ct);
            return Results.Created($"/clients/{created.Id}", created);
        });

        group.MapPut("/{id:guid}", async (
            IAppDbContext db, IValidator<UpdateClientRequest> validator, Guid id, UpdateClientRequest req, CancellationToken ct) =>
        {
            await validator.ValidateAndThrowAsync(req, ct);
            var updated = await ClientHandlers.UpdateAsync(db, id, req, ct);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        });

        group.MapDelete("/{id:guid}", async (IAppDbContext db, Guid id, CancellationToken ct) =>
            await ClientHandlers.DeleteAsync(db, id, ct) ? Results.NoContent() : Results.NotFound());

        return app;
    }
}
