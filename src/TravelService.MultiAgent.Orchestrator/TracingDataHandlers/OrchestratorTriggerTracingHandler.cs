using Microsoft.DurableTask;
using OpenTelemetry.Trace;
using System.Diagnostics;
using TravelService.MultiAgent.Orchestrator.Contracts;
using TravelService.MultiAgent.Orchestrator.Helper;
using TravelService.MultiAgent.Orchestrator.Models;

namespace TravelService.MultiAgent.Orchestrator.TracingDataHandlers
{
   public class OrchestratorTriggerTracingHandler : IOrchestratorTriggerTracingHandler
   {
      private readonly Tracer _tracer;
      private readonly TracingContextCache _itemsCache;
      public OrchestratorTriggerTracingHandler(TracerProvider tracerProvider, TracingContextCache itemsCache)
      {
         _tracer = tracerProvider.GetTracer("TravelService");
         _itemsCache = itemsCache;
      }
      private DateTime invocationStartTime { get; set; }


      public async Task<string> PopulateRootOrchestratorTracingData(Func<TaskOrchestrationContext, RequestData, Task<string?>> runManagerOrchestrator,
         TaskOrchestrationContext context,RequestData requestData)
      {
         ActivityTraceId parentTraceIdObj;
         ActivitySpanId parentSpanIdObj;
         ActivityTraceFlags activityTraceFlags;

         parentTraceIdObj = ActivityTraceId.CreateFromString(new ReadOnlySpan<char>(requestData.ParentTracingCache.GetValueOrDefault(OpenTelemetryConstants.TRACEID_KEY)?.ToString()?.ToCharArray()));
         parentSpanIdObj = ActivitySpanId.CreateFromString(new ReadOnlySpan<char>(requestData.ParentTracingCache.GetValueOrDefault(OpenTelemetryConstants.PARENT_SPANID_KEY)?.ToString()?.ToCharArray()));
         Enum.TryParse(requestData.ParentTracingCache.GetValueOrDefault(OpenTelemetryConstants.PARENT_SPAN_TRACEFLAG_KEY)?.ToString(),out activityTraceFlags);

         using var rootSpan = context.IsReplaying? _tracer.StartActiveSpan(context.Name, SpanKind.Internal,
             new SpanContext(parentTraceIdObj, parentSpanIdObj, activityTraceFlags)) : _tracer.StartSpan(context.Name, SpanKind.Internal,
             new SpanContext(parentTraceIdObj, parentSpanIdObj, activityTraceFlags));

         try
         {
            string traceId = rootSpan.Context.TraceId.ToString();
            string parentSpanId = rootSpan.Context.SpanId.ToString();
            string parentSpanTraceFlag = rootSpan.Context.TraceFlags.ToString();

            rootSpan.SetAttribute(OpenTelemetryConstants.ORCHESTRATOR_TRIGGER_KEY, "orchestrator-trigger-span");
            rootSpan.SetAttribute(OpenTelemetryConstants.ORCHESTRATOR_NAME_KEY, context.Name);
            rootSpan.SetAttribute(OpenTelemetryConstants.ORCHESTRATOR_INSTANCE_ID_KEY, context.InstanceId);

            DateTime invocationStartTime = DateTime.UtcNow;

            requestData.OrchestratorTracingCache.TryAdd(OpenTelemetryConstants.TRACEID_KEY, traceId);
            requestData.OrchestratorTracingCache.TryAdd(OpenTelemetryConstants.PARENT_SPANID_KEY, parentSpanId);
            requestData.OrchestratorTracingCache.TryAdd(OpenTelemetryConstants.PARENT_SPAN_TRACEFLAG_KEY, parentSpanTraceFlag);

            rootSpan.SetAttribute(OpenTelemetryConstants.OPERATION_STARTTIME_KEY, invocationStartTime.ToLongTimeString());

            var response = await runManagerOrchestrator(context, requestData);

            DateTime invocationCompletionTime = DateTime.UtcNow;
            rootSpan.SetAttribute(OpenTelemetryConstants.OPERATION_ENDTIME_KEY, invocationCompletionTime.ToLongTimeString());

            requestData.OrchestratorTracingCache[OpenTelemetryConstants.OPERATION_ENDTIME_KEY] = invocationCompletionTime.ToLongTimeString();

            return response;
         }
         catch (Exception ex)
         {
            rootSpan.RecordException(ex);
            throw;
         }

      }

      public async Task<RequestData> PopulateSubOrchestratorTracingData(Func<TaskOrchestrationContext, RequestData, Task<RequestData>> func,
         TaskOrchestrationContext context,RequestData requestData)
      {
         ActivityTraceId parentTraceIdObj;
         ActivitySpanId parentSpanIdObj;
         ActivityTraceFlags activityTraceFlags;       

         parentTraceIdObj = ActivityTraceId.CreateFromString(new ReadOnlySpan<char>(requestData.OrchestratorTracingCache.GetValueOrDefault(OpenTelemetryConstants.TRACEID_KEY)?.ToString()?.ToCharArray()));
         parentSpanIdObj = ActivitySpanId.CreateFromString(new ReadOnlySpan<char>(requestData.OrchestratorTracingCache.GetValueOrDefault(OpenTelemetryConstants.PARENT_SPANID_KEY)?.ToString()?.ToCharArray()));
         Enum.TryParse(requestData.OrchestratorTracingCache.GetValueOrDefault(OpenTelemetryConstants.PARENT_SPAN_TRACEFLAG_KEY)?.ToString(),out activityTraceFlags);

         using var childSpan = _tracer.StartActiveSpan(context.Name, SpanKind.Internal,
             new SpanContext(parentTraceIdObj, parentSpanIdObj, activityTraceFlags));         

         try
         {
            string traceId = childSpan.Context.TraceId.ToString();
            string parentSpanId = childSpan.Context.SpanId.ToString();
            string parentSpanTraceFlag = childSpan.Context.TraceFlags.ToString();

            childSpan.SetAttribute(OpenTelemetryConstants.ORCHESTRATOR_TRIGGER_KEY, "suborchestrator-trigger-span");
            childSpan.SetAttribute(OpenTelemetryConstants.ORCHESTRATOR_NAME_KEY, context.Name);

            requestData.SubOrchestratorTracingCache.TryAdd(OpenTelemetryConstants.TRACEID_KEY, traceId);
            requestData.SubOrchestratorTracingCache.TryAdd(OpenTelemetryConstants.PARENT_SPANID_KEY, parentSpanId);
            requestData.SubOrchestratorTracingCache.TryAdd(OpenTelemetryConstants.PARENT_SPAN_TRACEFLAG_KEY, parentSpanTraceFlag);
            DateTime invocationStartTime = DateTime.UtcNow;

            childSpan.SetAttribute(OpenTelemetryConstants.OPERATION_STARTTIME_KEY, invocationStartTime.ToLongTimeString());

            var response = await func(context, requestData);

            DateTime invocationCompletionTime = DateTime.UtcNow;

            childSpan.SetAttribute(OpenTelemetryConstants.OPERATION_ENDTIME_KEY, invocationCompletionTime.ToLongTimeString());            

            requestData.SubOrchestratorTracingCache[OpenTelemetryConstants.OPERATION_ENDTIME_KEY] = invocationCompletionTime.ToLongTimeString();

            return response;
         }
         catch (Exception ex)
         {
           childSpan.RecordException(ex);
            throw;
         }
      }
   }
}
