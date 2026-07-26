using System.Diagnostics;

namespace SeeSharp.Infrastructure.Telemetry;

public static class AppTelemetry
{
    public const string ActivitySourceName = "SeeSharp.Api";
    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
}
