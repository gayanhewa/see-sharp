using FluentValidation;
using SeeSharp.Application.Abstractions;
using SeeSharp.Application.Invoices;
using SeeSharp.Domain.Enums;
using SeeSharp.Infrastructure.Telemetry;

namespace SeeSharp.Api.Endpoints;

public static class InvoicesEndpoints
{
    public static WebApplication MapInvoices(this WebApplication app)
    {
        var group = app.MapGroup("/invoices").WithTags("Invoices");

        group.MapGet("/", async (
            IAppDbContext db, CancellationToken ct, string? status = null, Guid? clientId = null,
            int page = 1, int pageSize = 20) =>
        {
            InvoiceStatus? parsed = null;
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (!Enum.TryParse<InvoiceStatus>(status, ignoreCase: true, out var s))
                    return Results.BadRequest(new { error = $"Unknown status '{status}'." });
                parsed = s;
            }
            return Results.Ok(await InvoiceHandlers.ListAsync(db, parsed, clientId, page, pageSize, ct));
        });

        group.MapGet("/{id:guid}", async (IAppDbContext db, Guid id, CancellationToken ct) =>
        {
            var invoice = await InvoiceHandlers.GetAsync(db, id, ct);
            return invoice is null ? Results.NotFound() : Results.Ok(invoice);
        });

        group.MapPost("/", async (
            IAppDbContext db, IValidator<CreateInvoiceRequest> validator, AppMetrics metrics,
            CreateInvoiceRequest req, CancellationToken ct) =>
        {
            await validator.ValidateAndThrowAsync(req, ct);
            var created = await InvoiceHandlers.CreateAsync(db, req, ct);
            metrics.InvoiceCreated();
            return Results.Created($"/invoices/{created.Id}", created);
        });

        group.MapPut("/{id:guid}", async (
            IAppDbContext db, IValidator<UpdateInvoiceRequest> validator, Guid id, UpdateInvoiceRequest req, CancellationToken ct) =>
        {
            await validator.ValidateAndThrowAsync(req, ct);
            var updated = await InvoiceHandlers.UpdateAsync(db, id, req, ct);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        });

        group.MapPost("/{id:guid}/status", async (
            IAppDbContext db, Guid id, ChangeStatusRequest req, CancellationToken ct) =>
        {
            using var activity = AppTelemetry.ActivitySource.StartActivity("invoice.status_change");
            activity?.SetTag("invoice.id", id);
            activity?.SetTag("invoice.target_status", req.Status);

            var updated = await InvoiceHandlers.ChangeStatusAsync(db, id, req.Status, ct);
            activity?.SetTag("invoice.found", updated is not null);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        });

        group.MapDelete("/{id:guid}", async (IAppDbContext db, Guid id, CancellationToken ct) =>
            await InvoiceHandlers.DeleteAsync(db, id, ct) ? Results.NoContent() : Results.NotFound());

        return app;
    }
}
