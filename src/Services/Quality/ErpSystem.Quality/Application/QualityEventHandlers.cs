using ErpSystem.BuildingBlocks.Domain;
using ErpSystem.Quality.Domain;

namespace ErpSystem.Quality.Application;

/// <summary>
/// Integration Event Handlers for Quality Control
/// Triggers quality checks based on system events (e.g., procurement receipt, production order)
/// </summary>
public class QualityIntegrationEventHandlers
{
    private readonly IEventStore _eventStore;
    private readonly IQualityPointRepository _qualityPointRepository;
    private readonly ILogger<QualityIntegrationEventHandlers> _logger;

    public QualityIntegrationEventHandlers(
        IEventStore eventStore,
        IQualityPointRepository qualityPointRepository,
        ILogger<QualityIntegrationEventHandlers> logger)
    {
        _eventStore = eventStore;
        _qualityPointRepository = qualityPointRepository;
        _logger = logger;
    }

    /// <summary>
    /// Handle inventory receipt - creates mandatory IQC (Incoming Quality Control)
    /// </summary>
    public async Task HandleInventoryReceipt(InventoryReceiptIntegrationEvent @event)
    {
        _logger.LogInformation("Processing inventory receipt for quality check: {ReceiptId}", @event.ReceiptId);

        foreach (var item in @event.Items)
        {
            // Find mandatory quality points for this material and operation
            var qualityPoints = await _qualityPointRepository.GetPointsForMaterial(item.MaterialId, "RECEIPT");

            foreach (var point in qualityPoints)
            {
                var checkId = Guid.NewGuid();
                var check = QualityCheck.Create(
                    checkId,
                    @event.TenantId,
                    point.Id,
                    @event.ReceiptId,
                    "INVENTORY_RECEIPT",
                    item.MaterialId);

                await _eventStore.SaveAggregateAsync(check);

                _logger.LogInformation(
                    "Created IQC {CheckId} for Material {MaterialId} from Receipt {ReceiptId}",
                    checkId, item.MaterialId, @event.ReceiptId);
            }
        }
    }

    /// <summary>
    /// Handle production order start - creates mandatory PQC (Process Quality Control)
    /// </summary>
    public async Task HandleProductionOrderStarted(ProductionOrderStartedIntegrationEvent @event)
    {
        _logger.LogInformation("Processing production order for quality check: {OrderId}", @event.OrderId);

        var qualityPoints = await _qualityPointRepository.GetPointsForMaterial(@event.MaterialId, "PRODUCTION_START");

        foreach (var point in qualityPoints)
        {
            var checkId = Guid.NewGuid();
            var check = QualityCheck.Create(
                checkId,
                @event.TenantId,
                point.Id,
                @event.OrderId,
                "PRODUCTION_ORDER",
                @event.MaterialId);

            await _eventStore.SaveAggregateAsync(check);

            _logger.LogInformation(
                "Created PQC {CheckId} for Production Order {OrderId}",
                checkId, @event.OrderId);
        }
    }
}

// Interfaces and Events (Placeholders)
public interface IQualityPointRepository
{
    Task<List<QualityPoint>> GetPointsForMaterial(string materialId, string operationType);
}

public record InventoryReceiptIntegrationEvent(
    string TenantId,
    string ReceiptId,
    DateTime OccurredAt,
    List<ReceiptItem> Items);

public record ReceiptItem(string MaterialId, decimal Quantity);

public record ProductionOrderStartedIntegrationEvent(
    string TenantId,
    string OrderId,
    string MaterialId,
    DateTime OccurredAt);
