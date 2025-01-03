using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;

namespace TravelService.MultiAgent.Orchestrator.Helper
{
   public static class Extenstions
   {
      public static IServiceCollection AddOpenTelemetry(
       this IServiceCollection services,
       Action<OpenTelemetryLoggerOptions> configure = null)
      {
         if (services == null)
         {
            throw new ArgumentNullException(nameof(services));
         }

         services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, OpenTelemetryLoggerProvider>());

         if (configure != null)
         {
            services.Configure(configure);
         }

         return services;
      }

      public static void AddOpenTelemetryHeaders(this HttpClient client, IDictionary<string, object> itemsCache)
      {
         if (itemsCache == null || itemsCache.Count == 0)
            return;

         if (itemsCache.TryGetValue(OpenTelemetryConstants.TRACEID_KEY, out var traceId))
            client.DefaultRequestHeaders.Add(OpenTelemetryConstants.TRACEID_KEY, traceId.ToString());

         if (itemsCache.TryGetValue(OpenTelemetryConstants.PARENT_SPANID_KEY, out var parentSpanId))
            client.DefaultRequestHeaders.Add(OpenTelemetryConstants.PARENT_SPANID_KEY, parentSpanId.ToString());

         if (itemsCache.TryGetValue(OpenTelemetryConstants.PARENT_SPAN_TRACEFLAG_KEY, out var parentSpanTraceFlag))
            client.DefaultRequestHeaders.Add(OpenTelemetryConstants.PARENT_SPAN_TRACEFLAG_KEY, parentSpanTraceFlag.ToString());
      }
   }
}
