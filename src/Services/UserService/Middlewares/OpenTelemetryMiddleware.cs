using Microsoft.AspNetCore.Http;
using OpenTelemetry.Trace;
using static System.Net.Mime.MediaTypeNames;
using System.Diagnostics;
using System.Net;
using Newtonsoft.Json;
using UserService.Helpers;
using UserService.Models;

namespace UserService.Middlewares
{
   public class OpenTelemetryMiddleware
   {
      private readonly ILogger<OpenTelemetryMiddleware> _logger;
      private readonly RequestDelegate _next;
      private readonly Tracer _tracer;
      private TracingContextCache _cache;

      public OpenTelemetryMiddleware(RequestDelegate next, ILogger<OpenTelemetryMiddleware> logger, TracerProvider tracerProvider)
      {
         _next = next;
         _logger = logger;
         _tracer = tracerProvider.GetTracer("UserService");
      }

      public async Task Invoke(HttpContext httpContext, TracingContextCache cache)
      {
         _cache = cache;
         TelemetrySpan tempTelemetrySpan;

         _logger.LogInformation("Executed opentelemetry middleware");
         if (httpContext.Request.Headers.ContainsKey(OpenTelemetryConstants.TRACEID_KEY))
         {
            ActivityTraceId parentTraceIdObj = ActivityTraceId.CreateFromString(new ReadOnlySpan<char>(httpContext.Request.Headers["" + OpenTelemetryConstants.TRACEID_KEY].ToString()?.ToCharArray()));
            ActivitySpanId parentSpanIdObj = ActivitySpanId.CreateFromString(new ReadOnlySpan<char>(httpContext.Request.Headers["" + OpenTelemetryConstants.PARENT_SPANID_KEY].ToString()?.ToCharArray()));
            ActivityTraceFlags activityTraceFlags;
            bool parseResult = Enum.TryParse<ActivityTraceFlags>(httpContext.Request.Headers["" + OpenTelemetryConstants.PARENT_SPAN_TRACEFLAG_KEY].ToString(), out activityTraceFlags);
            tempTelemetrySpan = _tracer.StartActiveSpan("httptrigger-span", SpanKind.Server, new SpanContext(parentTraceIdObj, parentSpanIdObj, activityTraceFlags));
         }
         else
         {
            _logger.LogInformation("Tracer: " + JsonConvert.SerializeObject(_tracer));
            tempTelemetrySpan = _tracer.StartActiveSpan("httptrigger-span");
            _logger.LogInformation("telemetry span created: " + JsonConvert.SerializeObject(tempTelemetrySpan));
         }
         using var parentSpan = tempTelemetrySpan;
         try
         {
            _cache.Add(OpenTelemetryConstants.TRACEID_KEY, parentSpan.Context.TraceId.ToString());
            _cache.Add(OpenTelemetryConstants.PARENT_SPANID_KEY, parentSpan.Context.SpanId.ToString());
            _cache.Add(OpenTelemetryConstants.PARENT_SPAN_TRACEFLAG_KEY, parentSpan.Context.TraceFlags.ToString());

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
           
            await _next.Invoke(httpContext);
            DateTime invocationCompletionTime = DateTime.UtcNow;
            parentSpan.SetAttribute("EndTime", invocationCompletionTime.ToLongTimeString());
         }
         catch (Exception ex)
         {
            parentSpan?.RecordException(ex);
            httpContext.Response.ContentType = "application/json";
            httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            await httpContext.Response.WriteAsync(JsonConvert.SerializeObject(new { message = "Internal Server Error" }));
         }
      }

   }
}

