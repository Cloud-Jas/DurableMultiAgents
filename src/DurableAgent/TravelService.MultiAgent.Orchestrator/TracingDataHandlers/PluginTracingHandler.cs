using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;
using OpenTelemetry.Trace;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TravelService.MultiAgent.Orchestrator.Contracts;
using TravelService.MultiAgent.Orchestrator.Helper;
using TravelService.MultiAgent.Orchestrator.Models;

namespace TravelService.MultiAgent.Orchestrator.TracingDataHandlers
{
   public class PluginTracingHandler : IPluginTracingHandler
   {
      private readonly Tracer _tracer;
      private readonly TracingContextCache _itemsCache;

      public PluginTracingHandler(TracerProvider tracerProvider, TracingContextCache itemsCache)
      {
         _tracer = tracerProvider.GetTracer("TravelService");
         _itemsCache = itemsCache;
      }
      private DateTime invocationStartTime { get; set; }
      public async Task<string> ExecutePlugin(Func<Dictionary<string,string>, Task<string>> runPlugin, Dictionary<string, string> requestData)
      {
         ActivityTraceId parentTraceIdObj;
         ActivitySpanId parentSpanIdObj;
         ActivityTraceFlags activityTraceFlags;
         bool parseResult;
          
         parentTraceIdObj = ActivityTraceId.CreateFromString(new ReadOnlySpan<char>(_itemsCache.GetValueOrDefault(OpenTelemetryConstants.TRACEID_KEY)?.ToString()?.ToCharArray()));
         parentSpanIdObj = ActivitySpanId.CreateFromString(new ReadOnlySpan<char>(_itemsCache.GetValueOrDefault(OpenTelemetryConstants.PARENT_SPANID_KEY)?.ToString()?.ToCharArray()));
         Enum.TryParse(_itemsCache.GetValueOrDefault(OpenTelemetryConstants.PARENT_SPAN_TRACEFLAG_KEY)?.ToString(), out activityTraceFlags);

         using var childSpan = _tracer.StartActiveSpan(requestData["pluginName"], SpanKind.Server,
             new SpanContext(parentTraceIdObj, parentSpanIdObj, activityTraceFlags));

         try
         {
            string traceId = childSpan.Context.TraceId.ToString();
            string parentSpanId = childSpan.ParentSpanId.ToString();
            string parentSpanTraceFlag = childSpan.Context.TraceFlags.ToString();

            childSpan.SetAttribute(OpenTelemetryConstants.ACTIVITY_TRIGGER_KEY, "plugin-span");
            childSpan.SetAttribute(OpenTelemetryConstants.ACTIVITY_NAME_KEY, requestData["pluginName"]);
            childSpan.SetAttribute(OpenTelemetryConstants.ACTIVITY_INSTANCE_ID_KEY, _itemsCache[OpenTelemetryConstants.ACTIVITY_INSTANCE_ID_KEY].ToString());   
            childSpan.SetAttribute(OpenTelemetryConstants.REQUEST_DATA_KEY, JsonConvert.SerializeObject(requestData));

            invocationStartTime = DateTime.UtcNow;

            childSpan.SetAttribute(OpenTelemetryConstants.OPERATION_STARTTIME_KEY, invocationStartTime.ToLongTimeString());

            var response = await runPlugin(requestData);

            DateTime invocationCompletionTime = DateTime.UtcNow;
            double elapsedTime = (invocationCompletionTime - invocationCompletionTime).TotalMilliseconds;
            childSpan.SetAttribute(OpenTelemetryConstants.RESPONSE_DATA_KEY, JsonConvert.SerializeObject(response));
            childSpan.SetAttribute(OpenTelemetryConstants.OPERATION_ENDTIME_KEY, invocationCompletionTime.ToLongTimeString());
            childSpan.SetAttribute(OpenTelemetryConstants.RESPONSE_DATA_KEY, JsonConvert.SerializeObject(response));

            return response;
         }
         catch (Exception ex)
         {
            childSpan.RecordException(ex);
            return default;
         }
      }
   }
}
