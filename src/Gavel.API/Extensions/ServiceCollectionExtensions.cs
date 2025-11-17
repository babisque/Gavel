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
            .AddCustomCors()
            .AddSwagger()
            .AddRepositories()
            .AddApplicationServices();
    }

    private static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(opts =>
            opts.UseSqlServer(configuration.GetConnectionString("SqlServerConnection")));

        return services;
    }

    private static IServiceCollection AddCustomCors(this IServiceCollection services)
    {
        var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
        services.AddCors(options =>
        {
            options.AddPolicy(name: MyAllowSpecificOrigins,
                policy =>
                {
                    policy.WithOrigins("http://localhost:5173", "https://localhost:5173")
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
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IAuctionItemRepository, AuctionItemRepository>();
        return services;
    }

    private static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddAutoMapper(cfg =>
        {
            cfg.AddMaps(typeof(AuctionItemsMapper).Assembly);
        });
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(GetAuctionItemsHandler).Assembly));
        return services;
    }

}
