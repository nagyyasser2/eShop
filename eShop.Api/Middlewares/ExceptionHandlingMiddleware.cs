using System.Net;
using System.Text.Json;
using eShop.Core.Exceptions;

namespace eShop.Api.Middlewares
{
    public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        private readonly RequestDelegate _next = next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger = logger;

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context); 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception caught in middleware");

                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            HttpStatusCode statusCode;
            string message = ex.Message;

            // Map exception → HTTP status
            switch (ex)
            {
                case NotFoundException:
                    statusCode = HttpStatusCode.NotFound;
                    break;

                case BusinessRuleException:
                    statusCode = HttpStatusCode.BadRequest;
                    break;
                case InsufficientStockException:
                    statusCode = HttpStatusCode.Conflict;
                    break;
                case ForbiddenException:
                    statusCode = HttpStatusCode.Forbidden;
                    break;
                default:
                    statusCode = HttpStatusCode.InternalServerError;
                    message = "An unexpected error occurred."; // don’t expose internals
                    break;
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            var result = JsonSerializer.Serialize(new
            {
                error = message,
                statusCode = (int)statusCode,
                //traceId = context.TraceIdentifier // helps correlate logs & requests
            });

            return context.Response.WriteAsync(result);
        }
    }


}
