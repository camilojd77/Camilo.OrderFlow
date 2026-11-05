using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Camilo.OrderFlow.WebApi.Errors
{
    public sealed class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly IProblemDetailsService _problemDetailsService;

        public GlobalExceptionHandler(IProblemDetailsService problemDetailsService)
        {
            _problemDetailsService = problemDetailsService;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            // Mapear excepciones conocidas a status code
            var (status, title, type) = exception switch
            {
                // Se ajusta segun corresponda con excepciones reales
                UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized", "https://httpstatuses.com/401"),
                ForbiddenException => (StatusCodes.Status403Forbidden, "Forbidden", "https://httpstatuses.com/403"),
                NotFoundException => (StatusCodes.Status404NotFound, "Not Found", "https://httpstatuses.com/404"),
                ConflictException => (StatusCodes.Status409Conflict, "Conflict", "https://httpstatuses.com/409"),
                ValidationException => (StatusCodes.Status400BadRequest, "Validation Error", "https://httpstatuses.com/400"),
                _ => (StatusCodes.Status500InternalServerError, "Server Error", "https://httpstatuses.com/500")
            };

            var problem = new ProblemDetails
            {
                Status = status,
                Title = title,
                Type = type,
                Detail = status == 500
                    ? "An unexpected error occurred."
                    : exception.Message,
                Instance = httpContext.Request.Path
            };

            // Si es ValidationException, agrega errores por campo (opcional)
            if (exception is ValidationException vex && vex.Errors is not null)
            {
                problem.Extensions["errors"] = vex.Errors; // dictionary campo -> mensajes
            }

            httpContext.Response.StatusCode = status;

            return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = problem,
                Exception = exception
            });

        }
    }
}
