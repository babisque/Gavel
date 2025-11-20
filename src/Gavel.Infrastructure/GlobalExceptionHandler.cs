using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Gavel.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Gavel.Infrastructure;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "An unhandled exception occurred.");

        var problemDetails = new ProblemDetails
        {
            Instance = httpContext.Request.Path
        };

        switch (exception)
        {
            case NotFoundException notFound:
                problemDetails.Status = StatusCodes.Status404NotFound;
                problemDetails.Title = "Resource Not Found";
                problemDetails.Detail = notFound.Message;
                problemDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4";
                break;
            
            case ConflictException conflict:
                problemDetails.Status = StatusCodes.Status409Conflict;
                problemDetails.Title = "Conflict";
                problemDetails.Detail = conflict.Message;
                problemDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8";
                break;
            
            case ValidationException validation:
                problemDetails.Status = StatusCodes.Status400BadRequest;
                problemDetails.Title = "Validation error";
                problemDetails.Detail = validation.Message;
                problemDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
                
                var errors = validation.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray()
                    );
                problemDetails.Extensions.Add("errors", errors);
                break;
            
            
            default:
                problemDetails.Status = StatusCodes.Status500InternalServerError;
                problemDetails.Title = "Internal Server Error";
                problemDetails.Detail = "An unexpected error occurred.";
                break;
        }

        httpContext.Response.StatusCode = problemDetails.Status.Value;
        
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}