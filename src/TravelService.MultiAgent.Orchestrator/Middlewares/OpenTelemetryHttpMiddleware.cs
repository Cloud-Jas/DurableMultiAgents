
using AzureFunctions.Extensions.Middleware.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using OpenTelemetry.Trace;
using System.Diagnostics;
using Newtonsoft.Json;
using TravelService.MultiAgent.Orchestrator.Models;
using TravelService.MultiAgent.Orchestrator.Helper;

namespace TravelService.MultiAgent.Orchestrator.Middlewares
{
   public class OpenTelemetryHttpMiddleware : HttpMiddlewareBase
   {
      private readonly ILogger _logger;
      private readonly Tracer _tracer;
      private readonly TracingContextCache _itemsCache;

      public OpenTelemetryHttpMiddleware(ILogger logger, Tracer tracer, TracingContextCache itemsCache)
      {
         _logger = logger;
         _tracer = tracer;
         _itemsCache = itemsCache;
      }

      public override async Task InvokeAsync(HttpContext httpContext)
      {
         TelemetrySpan tempTelemetrySpan;
         if (httpContext != null && httpContext.Request != null && httpContext.Request.Headers != null)
         {
            _logger.LogInformation("Executed opentelemetry middleware");
            if (httpContext.Request.Headers.ContainsKey(OpenTelemetryConstants.TRACEID_KEY))
            {
               ActivityTraceId parentTraceIdObj = ActivityTraceId.CreateFromString(new ReadOnlySpan<char>(httpContext.Request.Headers["" + OpenTelemetryConstants.TRACEID_KEY].ToString()?.ToCharArray()));
               ActivitySpanId parentSpanIdObj = ActivitySpanId.CreateFromString(new ReadOnlySpan<char>(httpContext.Request.Headers["" + OpenTelemetryConstants.PARENT_SPANID_KEY].ToString()?.ToCharArray()));
               ActivityTraceFlags activityTraceFlags;
               bool parseResult = Enum.TryParse<ActivityTraceFlags>(httpContext.Request.Headers["" + OpenTelemetryConstants.PARENT_SPAN_TRACEFLAG_KEY].ToString(), out activityTraceFlags);
               tempTelemetrySpan = _tracer.StartActiveSpan("func-httptrigger-span", SpanKind.Server, new SpanContext(parentTraceIdObj, parentSpanIdObj, activityTraceFlags));
            }
            else
            {
               _logger.LogInformation("New tracer initialized for http trigger");
               _logger.LogInformation("Tracer: " + JsonConvert.SerializeObject(_tracer));
               tempTelemetrySpan = _tracer.StartRootSpan("func-httptrigger-span");
               _logger.LogInformation("telemetry span created: " + JsonConvert.SerializeObject(tempTelemetrySpan));
            }
            using var parentSpan = tempTelemetrySpan;
            try
            {              
               _logger.LogInformation("http request logging started in opentelemetry");

               parentSpan.SetAttribute("faas.trigger", "http");
               parentSpan.SetAttribute("http.method", httpContext.Request.Method);
               parentSpan.SetAttribute("http.http.host", httpContext.Request.Host.Value);
               parentSpan.SetAttribute("http.scheme", httpContext.Request.Scheme);
               parentSpan.SetAttribute("http.server_name", httpContext.Request.Host.Host);
               parentSpan.SetAttribute("net.host.port", httpContext.Request.Host.Port != null ? httpContext.Request.Host.Port.Value : 443);
               parentSpan.SetAttribute("http.route", httpContext.Request.Path);
               DateTime invocationStartTime = DateTime.UtcNow;
               parentSpan.SetAttribute("StartTime", invocationStartTime.ToLongTimeString());

               _logger.LogInformation("http request logged in opentelemetry");               

               httpContext.Request.Headers.Add(OpenTelemetryConstants.TRACEID_KEY, parentSpan.Context.TraceId.ToString());
               httpContext.Request.Headers.Add(OpenTelemetryConstants.PARENT_SPANID_KEY, parentSpan.Context.SpanId.ToString());
               httpContext.Request.Headers.Add(OpenTelemetryConstants.PARENT_SPAN_TRACEFLAG_KEY, parentSpan.Context.TraceFlags.ToString());

               await Next.InvokeAsync(httpContext);
               DateTime invocationCompletionTime = DateTime.UtcNow;
               parentSpan.SetAttribute("EndTime", invocationCompletionTime.ToLongTimeString());
            }
            catch (Exception ex)
            {
               parentSpan?.RecordException(ex);
               httpContext.Response.ContentType = "application/json";
               httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
               await httpContext.Response.WriteAsync(new
               {
                  StatusCode = httpContext.Response.StatusCode,
                  Message = ex.StackTrace + "\n" + ex.InnerException?.Message + "\n" + ex.Message
               }.ToString());
            }
         }
         else
         {
            _logger.LogInformation("Executed open telemetry middleware, httpcontext is null");
            await Next.InvokeAsync(httpContext);
         }

      }
   }
}