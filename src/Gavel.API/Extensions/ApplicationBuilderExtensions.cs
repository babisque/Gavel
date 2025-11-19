using System.ComponentModel.DataAnnotations;
using Gavel.API.Contracts;
using Gavel.API.Hubs;
using Gavel.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.HttpOverrides;

namespace Gavel.API.Extensions;

public static class ApplicationBuilderExtensions
{
    public static void UseApplicationPipeline(this WebApplication app)
    {
        var forwardedHeadersOptions = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        };

        forwardedHeadersOptions.KnownNetworks.Clear();
        forwardedHeadersOptions.KnownProxies.Clear();

        app.UseForwardedHeaders(forwardedHeadersOptions);

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Gavel API V1");
            });
        }

        app.UseExceptionHandler(exceptionHandlerApp =>
        {
            exceptionHandlerApp.Run(async context =>
            {
                var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
                if (exception is not null)
                {
                    context.Response.ContentType = "application/json";

                    context.Response.StatusCode = exception switch
                    {
                        NotFoundException => StatusCodes.Status404NotFound,
                        FluentValidation.ValidationException => StatusCodes.Status400BadRequest,
                        _ => StatusCodes.Status500InternalServerError
                    };

                    if (exception is FluentValidation.ValidationException validationException)
                    {
                        var errors = validationException.Errors.Select(e => 
                            new Domain.ErrorItem(e.ErrorCode, e.PropertyName, e.ErrorMessage));
    
                        var errorResponse = ApiResponseFactory.Failure<object>(errors);
                        await context.Response.WriteAsJsonAsync(errorResponse);
                    }
                    else
                    {
                        var errorResponse = ApiResponseFactory.Failure<object>("Error", exception.Message);
                        await context.Response.WriteAsJsonAsync(errorResponse);
                    }
                }
            });
        });

        app.UseCors("_myAllowSpecificOrigins");

        app.MapHub<BidHub>("hubs/bidHub");

        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
    }
}
