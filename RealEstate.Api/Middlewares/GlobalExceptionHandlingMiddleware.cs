using Microsoft.AspNetCore.Mvc;

namespace RealEstate.Api.Middlewares
{
    public sealed class GlobalExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

        public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (ArgumentException ex) // validaciones (MinPrice > MaxPrice, pageSize inválido, id vacío)
            {
                _logger.LogWarning(ex, "Bad request: {Message}", ex.Message);
                await WriteProblem(context, StatusCodes.Status400BadRequest, "Bad Request", ex.Message);
            }
            catch (KeyNotFoundException ex) // por si algún flujo lanza “no encontrado”
            {
                _logger.LogInformation(ex, "Not found: {Message}", ex.Message);
                await WriteProblem(context, StatusCodes.Status404NotFound, "Not Found", ex.Message);
            }
            catch (Exception ex) // todo lo demás
            {
                _logger.LogError(ex, "Unhandled exception");
                await WriteProblem(context, StatusCodes.Status500InternalServerError, "Server Error", "Unexpected error");
            }
        }

        private static Task WriteProblem(HttpContext ctx, int status, string title, string detail)
        {
            var pd = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = detail,
                Instance = ctx.Request?.Path.Value
            };
            pd.Extensions["traceId"] = ctx.TraceIdentifier;

            ctx.Response.StatusCode = status;

            return Results.Problem(title: title, detail: detail, statusCode: status)
              .ExecuteAsync(ctx);
        }
    }
}
