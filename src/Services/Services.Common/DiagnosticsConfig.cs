using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Services.Common
{
    public class DiagnosticsConfig
    {
        public DiagnosticsConfig(string name)
        {
            Tracing = new ActivitySource(name);
            Metrics = new Meter(name);
        }

        /// <summary>
        /// https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/docs/trace/getting-started-aspnetcore
        /// </summary>
        internal ActivitySource Tracing { get; }

        /// <summary>
        /// https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/docs/metrics/getting-started-aspnetcore
        /// </summary>
        internal Meter Metrics { get; }

        public void Dispose()
        {
            Metrics.Dispose();
            Tracing.Dispose();
        }
    }
}
