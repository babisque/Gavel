using System.Text;
using System.Text.Json.Serialization;
using FluentValidation;
using Gavel.API.Services;
using Gavel.Application.Behaviors;
using Gavel.Application.Handlers.AuctionItem.GetAuctionItems;
using Gavel.Application.Handlers.Bids.PlaceBid;
using Gavel.Application.Profiles;
using Gavel.Domain.Entities;
using Gavel.Domain.Interfaces.Services;
using Gavel.Infrastructure;
using Gavel.Infrastructure.BackgroundServices;
using Gavel.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Quartz;

namespace Gavel.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddApplicationCore(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddDatabase(configuration)
            .AddIdentity()
            .AddJwtAuthentication(configuration)
            .AddCustomCors(configuration)
            .AddSwagger()
            .AddApplicationServices(configuration)
            .AddQuartzConfiguration(configuration)
            .AddExceptionHandling()
            .AddSignalR()
            .AddJsonProtocol(opts =>
            {
                opts.PayloadSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            });
    }

    private static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(opts =>
            opts.UseSqlServer(configuration.GetConnectionString("SqlServerConnection")));

        return services;
    }

    private static IServiceCollection AddIdentity(this IServiceCollection services)
    {
        services.AddIdentityCore<ApplicationUser>(opts =>
            {
                opts.Password.RequireDigit = false;
                opts.Password.RequiredLength = 6;
                opts.Password.RequireNonAlphanumeric = false;
                opts.Password.RequireUppercase = false;
                opts.Password.RequireLowercase = false;
                opts.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

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
                        .AllowAnyMethod()
                        .AllowCredentials();
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
        services.AddScoped<ITokenService, TokenService>();
        
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

    public static IServiceCollection AddExceptionHandling(this IServiceCollection services)
    {
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        return services;
    }

    private static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var key = Encoding.ASCII.GetBytes(configuration["Jwt:Secret"] ??
                                          throw new InvalidOperationException("JWT Key not found in configuration."));
        services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(x =>
            {
                x.RequireHttpsMetadata = false;
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = configuration["Jwt:Audience"],
                };

                x.Events = new JwtBearerEvents
                {
                    OnMessageReceived = ctx =>
                    {
                        var accessToken = ctx.Request.Query["access_token"];
                        var path = ctx.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                            ctx.Token = accessToken;

                        return Task.CompletedTask;
                    }
                };
            });
        
        return services;
    }
}
