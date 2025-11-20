using System.ComponentModel.DataAnnotations;
using Gavel.API.Contracts;
using Gavel.API.Hubs;
using Gavel.Domain.Exceptions;
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

        app.UseExceptionHandler();

        app.UseCors("_myAllowSpecificOrigins");

        app.MapHub<BidHub>("hubs/bidHub");

        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
    }
}
