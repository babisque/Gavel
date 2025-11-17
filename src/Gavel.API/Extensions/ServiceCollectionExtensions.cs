using Gavel.Application.Handlers.AuctionItem.GetAuctionItems;
using Gavel.Application.Profiles;
using Gavel.Domain.Interfaces;
using Gavel.Domain.Interfaces.Repositories;
using Gavel.Infrastructure;
using Gavel.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Gavel.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddApplicationCore(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddDatabase(configuration)
            .AddCustomCors(configuration)
            .AddSwagger()
            .AddRepositories()
            .AddApplicationServices(configuration);
    }

    private static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(opts =>
            opts.UseSqlServer(configuration.GetConnectionString("SqlServerConnection")));

        return services;
    }

    private static IServiceCollection AddCustomCors(this IServiceCollection services, IConfiguration configuration)
    {
        var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
        
        var origins = configuration.GetValue<string>("Cors:AllowedOrigins")?.Split(',') ?? [];
        
        services.AddCors(options =>
        {
            options.AddPolicy(name: MyAllowSpecificOrigins,
                policy =>
                {
                    policy.WithOrigins(origins)
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
        });

        return services;
    }

    private static IServiceCollection AddSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo { Title = "Gavel API", Version = "v1" });
        });

        return services;
    }

    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IAuctionItemRepository, AuctionItemRepository>();
        return services;
    }

    private static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAutoMapper(cfg =>
        {
            cfg.AddMaps(typeof(AuctionItemsMapper).Assembly);
            cfg.LicenseKey = configuration.GetSection("LuckyPenny:LicenseKey").Value;
        });
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(GetAuctionItemsHandler).Assembly);
            cfg.LicenseKey = configuration.GetSection("LuckyPenny:LicenseKey").Value;
        });
        return services;
    }

}
