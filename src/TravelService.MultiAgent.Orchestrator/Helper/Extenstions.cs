using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
   }
}
