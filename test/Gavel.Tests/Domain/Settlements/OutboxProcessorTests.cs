using System.Text.Json;
using Gavel.Api.Features.Settlements.Services;
using Gavel.Api.Infrastructure.Data;
using Gavel.Core.Infrastructure.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using TUnit.Core;
using static TUnit.Assertions.Assert;

namespace Gavel.Tests.Domain.Settlements;

public class OutboxProcessorTests : IDisposable
{
    private readonly string _dbName = Guid.NewGuid().ToString();
    private readonly IAuditLogger _auditLogger;
    private readonly ServiceProvider _serviceProvider;
    private readonly TimeProvider _timeProvider;
    private readonly IOptions<LotClosingOptions> _options;

    public OutboxProcessorTests()
    {
        _auditLogger = Substitute.For<IAuditLogger>();
        _timeProvider = Substitute.For<TimeProvider>();
        _timeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow);

        var services = new ServiceCollection();
        services.AddDbContext<GavelDbContext>(options => 
            options.UseSqlite($"Data Source={_dbName}.db"));
        services.AddSingleton(_auditLogger);
        services.AddSingleton(_timeProvider);
        _serviceProvider = services.BuildServiceProvider();

        using (var scope = _serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<GavelDbContext>();
            context.Database.EnsureCreated();
        }

        _options = Options.Create(new LotClosingOptions
        {
            OutboxBatchSize = 10,
            OutboxCheckInterval = TimeSpan.FromSeconds(1)
        });
    }

    [Test]
    public async Task OutboxProcessor_ProcessesAuditRecord_Successfully()
    {
        // Arrange
        var record = new AuditRecord(Guid.NewGuid(), "TestEvent", DateTimeOffset.UtcNow, "Metadata");
        var message = new OutboxMessage
        {
            Type = "AuditRecord",
            Content = JsonSerializer.Serialize(record, AppJsonSerializerContext.Default.AuditRecord),
            CreatedAt = DateTimeOffset.UtcNow
        };

        using (var scope = _serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<GavelDbContext>();
            context.OutboxMessages.Add(message);
            await context.SaveChangesAsync();
        }

        var logger = Substitute.For<ILogger<OutboxProcessorBackgroundService>>();
        var processor = new OutboxProcessorBackgroundService(_serviceProvider, _options, _timeProvider, logger);

        // Act
        var method = typeof(OutboxProcessorBackgroundService)
            .GetMethod("ProcessOutboxMessagesAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        await (Task)method!.Invoke(processor, new object[] { CancellationToken.None })!;

        // Assert
        await _auditLogger.Received(1).LogAsync(Arg.Any<AuditRecord>());
        
        using (var scope = _serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<GavelDbContext>();
            var updatedMessage = await context.OutboxMessages.FindAsync(message.Id);
            await That(updatedMessage!.ProcessedAt).IsNotNull();
            await That(updatedMessage.RetryCount).IsEqualTo(0);
            await That(updatedMessage.ErrorMessage).IsNull();
        }
    }

    [Test]
    public async Task OutboxProcessor_IncrementsRetryCount_OnFailure()
    {
        // Arrange
        _auditLogger.When(x => x.LogAsync(Arg.Any<AuditRecord>()))
            .Do(x => throw new Exception("Simulated Failure"));

        var record = new AuditRecord(Guid.NewGuid(), "FailEvent", DateTimeOffset.UtcNow, "Metadata");
        var message = new OutboxMessage
        {
            Type = "AuditRecord",
            Content = JsonSerializer.Serialize(record, AppJsonSerializerContext.Default.AuditRecord),
            CreatedAt = DateTimeOffset.UtcNow
        };

        using (var scope = _serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<GavelDbContext>();
            context.OutboxMessages.Add(message);
            await context.SaveChangesAsync();
        }

        var logger = Substitute.For<ILogger<OutboxProcessorBackgroundService>>();
        var processor = new OutboxProcessorBackgroundService(_serviceProvider, _options, _timeProvider, logger);

        var method = typeof(OutboxProcessorBackgroundService)
            .GetMethod("ProcessOutboxMessagesAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        await (Task)method!.Invoke(processor, new object[] { CancellationToken.None })!;

        // Assert
        using (var scope = _serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<GavelDbContext>();
            var updatedMessage = await context.OutboxMessages.FindAsync(message.Id);
            await That(updatedMessage!.ProcessedAt).IsNull();
            await That(updatedMessage.RetryCount).IsEqualTo(1);
            await That(updatedMessage.ErrorMessage).IsEqualTo("Simulated Failure");
        }
    }

    public void Dispose()
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<GavelDbContext>();
            context.Database.EnsureDeleted();
        }
        _serviceProvider.Dispose();
        
        if (File.Exists($"{_dbName}.db"))
        {
            File.Delete($"{_dbName}.db");
        }
    }
}
