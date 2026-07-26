using System.Diagnostics.Metrics;

namespace SeeSharp.Infrastructure.Telemetry;

public sealed class AppMetrics
{
    public const string MeterName = "SeeSharp.Api";
    private readonly Counter<long> _invoicesCreated;

    public AppMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _invoicesCreated = meter.CreateCounter<long>("invoices_created", description: "Number of invoices created.");
    }

    public void InvoiceCreated() => _invoicesCreated.Add(1);
}
