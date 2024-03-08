using OpenTelemetry;
using System.Diagnostics;

namespace Basket.API.Observability
{
    /// <summary>
    /// https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/docs/trace/extending-the-sdk
    /// </summary>
    public class TenantIdProcessor : BaseProcessor<Activity>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TenantIdProcessor(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public override void OnStart(Activity data)
        {
            var tenantId = _httpContextAccessor.HttpContext?.Request.Headers["x-tenant-id"].ToString();
            if (string.IsNullOrEmpty(tenantId))
                return;

            data.SetTag("tenant.id", tenantId);
        }
    }
}
