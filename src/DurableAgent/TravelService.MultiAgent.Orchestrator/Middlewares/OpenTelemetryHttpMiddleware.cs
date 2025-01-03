using AzureFunctions.Extensions.Middleware.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Diagnostics;
using OpenTelemetry.Trace;
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
         if (httpContext?.Request?.Headers == null)
         {
            _logger.LogWarning("HttpContext or headers are null.");
            await Next.InvokeAsync(httpContext);
            return;
         }

         _logger.LogInformation("Executing OpenTelemetry middleware.");

         var parentSpan = CreateTelemetrySpan(httpContext);
         using (parentSpan)
         {
            try
            {
               LogRequestAttributes(httpContext, parentSpan);
               AddTraceHeaders(httpContext, parentSpan);
               await Next.InvokeAsync(httpContext);
               parentSpan.SetAttribute("EndTime", DateTime.UtcNow.ToLongTimeString());
            }
            catch (Exception ex)
            {
               RecordException(parentSpan, httpContext, ex);
            }
         }
      }

      private TelemetrySpan CreateTelemetrySpan(HttpContext httpContext)
      {
         if (httpContext.Request.Headers.TryGetValue(OpenTelemetryConstants.TRACEID_KEY, out var traceId) &&
             httpContext.Request.Headers.TryGetValue(OpenTelemetryConstants.PARENT_SPANID_KEY, out var spanId) &&
             httpContext.Request.Headers.TryGetValue(OpenTelemetryConstants.PARENT_SPAN_TRACEFLAG_KEY, out var traceFlags))
         {
            return _tracer.StartActiveSpan(
                "func-httptrigger-span",
                SpanKind.Server,
                new SpanContext(
                    ActivityTraceId.CreateFromString(traceId.ToString().AsSpan()),
                    ActivitySpanId.CreateFromString(spanId.ToString().AsSpan()),
                    Enum.TryParse<ActivityTraceFlags>(traceFlags.ToString(), out var flags) ? flags : ActivityTraceFlags.None));
         }

         _logger.LogInformation("Initializing new trace.");
         return _tracer.StartRootSpan("func-httptrigger-span");
      }

      private void LogRequestAttributes(HttpContext httpContext, TelemetrySpan span)
      {
         span.SetAttribute("faas.trigger", "http");
         span.SetAttribute("http.method", httpContext.Request.Method);
         span.SetAttribute("http.host", httpContext.Request.Host.Value);
         span.SetAttribute("http.scheme", httpContext.Request.Scheme);
         span.SetAttribute("http.server_name", httpContext.Request.Host.Host);
         span.SetAttribute("net.host.port", httpContext.Request.Host.Port ?? 443);
         span.SetAttribute("http.route", httpContext.Request.Path);
         span.SetAttribute("StartTime", DateTime.UtcNow.ToLongTimeString());
      }

      private void AddTraceHeaders(HttpContext httpContext, TelemetrySpan span)
      {
         _itemsCache.Clear();
         _itemsCache.TryAdd(OpenTelemetryConstants.TRACEID_KEY, span.Context.TraceId.ToString());
         _itemsCache.TryAdd(OpenTelemetryConstants.PARENT_SPANID_KEY, span.Context.SpanId.ToString());
         _itemsCache.TryAdd(OpenTelemetryConstants.PARENT_SPAN_TRACEFLAG_KEY, span.Context.TraceFlags.ToString());

         httpContext.Request.Headers[OpenTelemetryConstants.TRACEID_KEY] = span.Context.TraceId.ToString();
         httpContext.Request.Headers[OpenTelemetryConstants.PARENT_SPANID_KEY] = span.Context.SpanId.ToString();
         httpContext.Request.Headers[OpenTelemetryConstants.PARENT_SPAN_TRACEFLAG_KEY] = span.Context.TraceFlags.ToString();
      }

      private async Task RecordException(TelemetrySpan span, HttpContext httpContext, Exception ex)
      {
         span.RecordException(ex);
         _logger.LogError(ex, "An error occurred in OpenTelemetry middleware.");

         httpContext.Response.ContentType = "application/json";
         httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

         var errorResponse = new
         {
            StatusCode = httpContext.Response.StatusCode,
            Message = $"{ex.StackTrace}\n{ex.InnerException?.Message}\n{ex.Message}"
         };

         await httpContext.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(errorResponse));
      }
   }
}
