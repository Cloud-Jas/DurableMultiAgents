using AzureFunctions.Extensions.Middleware.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Threading.Tasks;

namespace TravelService.MultiAgent.Orchestrator.Middlewares
{
   public class HttpExceptionMiddleware : HttpMiddlewareBase
   {
      private readonly ILogger _logger;
      public HttpExceptionMiddleware(ILogger logger)
      {
         _logger = logger;
      }

      public override async Task InvokeAsync(HttpContext httpContext)
      {
         if (httpContext != null)
         {
            _logger.LogInformation("Executed http exeception middleware");
            try
            {
               await Next.InvokeAsync(httpContext);
            }
            catch (Exception ex)
            {
               _logger.LogInformation("exception occured while processing the request");

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
            _logger.LogInformation("Executed http exeception middleware, httpcontext is null");
            await Next.InvokeAsync(httpContext);
         }
      }
   }
}
