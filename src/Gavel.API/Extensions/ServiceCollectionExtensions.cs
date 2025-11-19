using FluentValidation;
using Gavel.API.Services;
using Gavel.Application.Behaviors;
using Gavel.Application.Handlers.AuctionItem.GetAuctionItems;
using Gavel.Application.Handlers.Bid.PlaceBid;
using Gavel.Application.Interfaces;
using Gavel.Application.Profiles;
using Gavel.Domain.Interfaces;
using Gavel.Infrastructure;
using Gavel.Infrastructure.BackgroundServices;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Quartz;

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
            .AddApplicationServices(configuration)
            .AddQuartzConfiguration(configuration)
            .AddSignalR();
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
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Gavel API", Version = "v1" });
        });

        return services;
    }

    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        return services;
    }

    private static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddValidatorsFromAssembly(typeof(PlaceBidCommand).Assembly);
        
        services.AddAutoMapper(cfg =>
        {
            cfg.AddMaps(typeof(AuctionItemsMapper).Assembly);
            cfg.LicenseKey = configuration.GetSection("LuckyPenny:LicenseKey").Value;
        });
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(GetAuctionItemsHandler).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.LicenseKey = configuration.GetSection("LuckyPenny:LicenseKey").Value;
        });
        services.AddScoped<IBidNotificationService, SignalRBidNotificationService>();
        services.AddHostedService<OutboxProcessor>();
        
        return services;
    }

    private static IServiceCollection AddQuartzConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<QuartzOptions>(configuration.GetSection("Quartz"));
        
        services.Configure<QuartzOptions>(opts =>
        {
            opts.Scheduling.IgnoreDuplicates = true;
            opts.Scheduling.OverWriteExistingData = true;
        });

        services.AddQuartz(q =>
        {
            q.SchedulerId = "QuartzScheduler";

            q.UseSimpleTypeLoader();
            
            q.UsePersistentStore(s =>
            {
                s.UseProperties = true;
                
                s.UseSqlServer(sqlServer =>
                {
                    sqlServer.ConnectionString = configuration.GetConnectionString("SqlServerConnection");
                });
                
                s.UseJsonSerializer();
            });
            
            q.UseDefaultThreadPool(tp =>
            {
                tp.MaxConcurrency = 10;
            });
        });
        
        return services;
    }
}
