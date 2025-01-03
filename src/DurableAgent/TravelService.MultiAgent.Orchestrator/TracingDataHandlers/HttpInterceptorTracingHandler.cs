using TravelService.MultiAgent.Orchestrator.Helper;
using TravelService.MultiAgent.Orchestrator.Models;

namespace TravelService.MultiAgent.Orchestrator.TracingDataHandlers
{
   public class HttpInterceptorTracingHandler : DelegatingHandler
   {
      private readonly TracingContextCache _itemsCache;
      public HttpInterceptorTracingHandler(TracingContextCache itemsCache)
      {
         _itemsCache = itemsCache;
      }

      protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
      {
         request.Headers.Add(OpenTelemetryConstants.TRACEID_KEY,_itemsCache[OpenTelemetryConstants.TRACEID_KEY].ToString());
         request.Headers.Add(OpenTelemetryConstants.PARENT_SPANID_KEY, _itemsCache[OpenTelemetryConstants.PARENT_SPANID_KEY].ToString());
         request.Headers.Add(OpenTelemetryConstants.PARENT_SPAN_TRACEFLAG_KEY, _itemsCache[OpenTelemetryConstants.PARENT_SPAN_TRACEFLAG_KEY].ToString());
         return await base.SendAsync(request, cancellationToken);
      }
   }
}
