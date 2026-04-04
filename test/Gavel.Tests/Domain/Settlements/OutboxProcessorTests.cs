using System.Text.Json;
using Gavel.Api.Features.Settlements.Services;
using Gavel.Api.Infrastructure.Data;
using Gavel.Core.Domain.Settlements;
using Gavel.Core.Infrastructure.Logging;
using Gavel.Core.Infrastructure.Legal;
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
    private readonly IDigitalSignatureService _signatureService;
    private readonly ServiceProvider _serviceProvider;
    private readonly TimeProvider _timeProvider;
    private readonly IOptions<LotClosingOptions> _options;

    public OutboxProcessorTests()
    {
        _auditLogger = Substitute.For<IAuditLogger>();
        _signatureService = Substitute.For<IDigitalSignatureService>();
        _timeProvider = Substitute.For<TimeProvider>();
        _timeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow);

        var services = new ServiceCollection();
        services.AddDbContext<GavelDbContext>(options => 
            options.UseSqlite($"Data Source={_dbName}.db"));
        services.AddSingleton(_auditLogger);
        services.AddSingleton(_signatureService);
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
            CreatedAt = DateTimeOffset.UtcNow,
            Status = OutboxMessageStatus.Pending
        };

        var messageId = message.Id;

        using (var scope = _serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<GavelDbContext>();
            context.OutboxMessages.Add(message);
            await context.SaveChangesAsync();
        }

        // We register the singleton services directly in the processor for verification
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
            var updatedMessage = await context.OutboxMessages.FindAsync(messageId);
            await That(updatedMessage!.Status).IsEqualTo(OutboxMessageStatus.Completed);
            await That(updatedMessage.ProcessedAt).IsNotNull();
        }
    }

    [Test]
    public async Task OutboxProcessor_ProcessesSignSettlement_Successfully()
    {
        // Arrange
        var settlementId = Guid.NewGuid();
        var settlement = new Settlement(
            settlementId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new Gavel.Core.Domain.Lots.PriceBreakdown(1000, 50, 50, 1100),
            DateTimeOffset.UtcNow,
            SettlementStatus.PendingSignature
        );

        var message = new OutboxMessage
        {
            Type = "SignSettlement",
            Content = settlementId.ToString(),
            CreatedAt = DateTimeOffset.UtcNow,
            Status = OutboxMessageStatus.Pending
        };

        var messageId = message.Id;

        using (var scope = _serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<GavelDbContext>();
            context.Settlements.Add(settlement);
            context.OutboxMessages.Add(message);
            await context.SaveChangesAsync();
        }

        _signatureService.SignSettlementAsync(Arg.Any<Settlement>()).Returns("valid-signature");

        var logger = Substitute.For<ILogger<OutboxProcessorBackgroundService>>();
        var processor = new OutboxProcessorBackgroundService(_serviceProvider, _options, _timeProvider, logger);

        // Act
        var method = typeof(OutboxProcessorBackgroundService)
            .GetMethod("ProcessOutboxMessagesAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        await (Task)method!.Invoke(processor, new object[] { CancellationToken.None })!;

        // Assert
        await _signatureService.Received(1).SignSettlementAsync(Arg.Any<Settlement>());
        
        using (var scope = _serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<GavelDbContext>();
            var updatedSettlement = await context.Settlements.FindAsync(settlementId);
            await That(updatedSettlement!.Status).IsEqualTo(SettlementStatus.Signed);
            await That(updatedSettlement.DigitalSignature).IsEqualTo("valid-signature");
            
            var updatedMessage = await context.OutboxMessages.FindAsync(messageId);
            await That(updatedMessage!.Status).IsEqualTo(OutboxMessageStatus.Completed);
            await That(updatedMessage.ProcessedAt).IsNotNull();
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
