using Azure.AI.Inference;
using Microsoft.Azure.Functions.Worker;
using Microsoft.SemanticKernel;
using Newtonsoft.Json;
using OpenTelemetry.Trace;
using System.Diagnostics;
using TravelService.MultiAgent.Orchestrator.Agents;
using TravelService.MultiAgent.Orchestrator.Contracts;
using TravelService.MultiAgent.Orchestrator.Helper;
using TravelService.MultiAgent.Orchestrator.Models;

namespace TravelService.MultiAgent.Orchestrator.TracingDataHandlers
{
   public class ActivityTriggerTracingHandler : IActivityTriggerTracingHandler
   {
      private readonly Tracer _tracer;
      private readonly TracingContextCache _itemsCache;

      public ActivityTriggerTracingHandler(TracerProvider tracerProvider, TracingContextCache itemsCache)
      {
         _tracer = tracerProvider.GetTracer("TravelService");
         _itemsCache = itemsCache;
      }
      private DateTime invocationStartTime { get; set; }
      public async Task<TResponse> ExecuteActivityTrigger<TResponse>(Func<RequestData, FunctionContext, Task<TResponse>> runActivityTrigger, RequestData requestData, FunctionContext executionContext)
      {
         ActivityTraceId parentTraceIdObj;
         ActivitySpanId parentSpanIdObj;         
         ActivityTraceFlags activityTraceFlags;
         bool parseResult;

         parentTraceIdObj = ActivityTraceId.CreateFromString(new ReadOnlySpan<char>(requestData.ParentTracingCache.GetValueOrDefault(OpenTelemetryConstants.TRACEID_KEY)?.ToString()?.ToCharArray()));
         parentSpanIdObj = ActivitySpanId.CreateFromString(new ReadOnlySpan<char>(requestData.ParentTracingCache.GetValueOrDefault(OpenTelemetryConstants.PARENT_SPANID_KEY)?.ToString()?.ToCharArray()));
         Enum.TryParse(requestData.ParentTracingCache.GetValueOrDefault(OpenTelemetryConstants.PARENT_SPAN_TRACEFLAG_KEY)?.ToString(), out activityTraceFlags);

         using var childSpan = _tracer.StartSpan(executionContext.FunctionDefinition.Name, SpanKind.Server,
             new SpanContext(parentTraceIdObj, parentSpanIdObj, activityTraceFlags));

         try
         {
            string traceId = childSpan.Context.TraceId.ToString();
            string parentSpanId = childSpan.Context.SpanId.ToString();
            string parentSpanTraceFlag = childSpan.Context.TraceFlags.ToString();

            childSpan.SetAttribute(OpenTelemetryConstants.ACTIVITY_TRIGGER_KEY, "activity-trigger-span");
            childSpan.SetAttribute(OpenTelemetryConstants.ACTIVITY_NAME_KEY, executionContext.FunctionDefinition.Name);
            childSpan.SetAttribute(OpenTelemetryConstants.ACTIVITY_INSTANCE_ID_KEY, executionContext.FunctionDefinition.Id);

            _itemsCache.Clear();

            _itemsCache.Add(OpenTelemetryConstants.ACTIVITY_INSTANCE_ID_KEY, executionContext.FunctionDefinition.Id);

            _itemsCache.TryAdd(OpenTelemetryConstants.TRACEID_KEY, traceId);
            _itemsCache.TryAdd(OpenTelemetryConstants.PARENT_SPANID_KEY, parentSpanId);
            _itemsCache.TryAdd(OpenTelemetryConstants.PARENT_SPAN_TRACEFLAG_KEY, parentSpanTraceFlag);

            invocationStartTime = DateTime.UtcNow;

            childSpan.SetAttribute(OpenTelemetryConstants.OPERATION_STARTTIME_KEY, invocationStartTime.ToLongTimeString());

            var response = await runActivityTrigger(requestData, executionContext);

            DateTime invocationCompletionTime = DateTime.UtcNow;
            double elapsedTime = (invocationCompletionTime - invocationCompletionTime).TotalMilliseconds;            

            if(response is ChatMessageContent)
            {
               var chatMessageContent = response as ChatMessageContent;

               childSpan.SetAttribute(OpenTelemetryConstants.RESPONSE_DATA_KEY, JsonConvert.SerializeObject(chatMessageContent!.Content));
               var usage = chatMessageContent.Metadata;
               childSpan.SetAttribute(OpenTelemetryConstants.TOKEN_CONSUMPION_KEY, JsonConvert.SerializeObject(usage));
            }
            else
            {
               childSpan.SetAttribute(OpenTelemetryConstants.RESPONSE_DATA_KEY, JsonConvert.SerializeObject(response));
            }

            childSpan.SetAttribute(OpenTelemetryConstants.OPERATION_ENDTIME_KEY, invocationCompletionTime.ToLongTimeString());

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
