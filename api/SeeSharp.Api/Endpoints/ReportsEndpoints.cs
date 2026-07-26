using SeeSharp.Application.Abstractions;
using SeeSharp.Application.Reports;

namespace SeeSharp.Api.Endpoints;

public static class ReportsEndpoints
{
    public static WebApplication MapReports(this WebApplication app)
    {
        var group = app.MapGroup("/reports").WithTags("Reports");

        group.MapGet("/summary", async (IAppDbContext db, DateOnly? from, DateOnly? to, CancellationToken ct) =>
        {
            var fromDate = from ?? new DateOnly(DateTime.UtcNow.Year, 1, 1);
            var toDate = to ?? DateOnly.FromDateTime(DateTime.UtcNow);
            if (toDate < fromDate)
                return Results.BadRequest(new { error = "'to' cannot be before 'from'." });
            return Results.Ok(await ReportHandlers.GetSummaryAsync(db, fromDate, toDate, ct));
        });

        return app;
    }
}
