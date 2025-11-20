using Gavel.API.Hubs;
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
        app.UseStatusCodePages();

        app.UseCors("_myAllowSpecificOrigins");

        app.MapHub<BidHub>("hubs/bidHub");

        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
    }
}
