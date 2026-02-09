using ErpSystem.BuildingBlocks.Domain;
using ErpSystem.Quality.Domain;
using MediatR;
using ErpSystem.Procurement.Domain;
using ErpSystem.Production.Domain;

namespace ErpSystem.Quality.Application;

/// <summary>
/// Integration Event Handlers for Quality Control
/// Triggers quality checks based on system events (e.g., procurement receipt, production order)
/// </summary>
public class QualityIntegrationEventHandlers(
    IEventStore eventStore,
    IQualityPointRepository qualityPointRepository,
    ILogger<QualityIntegrationEventHandlers> logger) :
    INotificationHandler<ProcurementIntegrationEvents.GoodsReceivedIntegrationEvent>,
    INotificationHandler<ProductionIntegrationEvents.ProductionOrderReleasedIntegrationEvent>
{
    /// <summary>
    /// Handle inventory receipt - creates mandatory IQC (Incoming Quality Control)
    /// </summary>
    public async Task Handle(ProcurementIntegrationEvents.GoodsReceivedIntegrationEvent @event, CancellationToken ct)
    {
        logger.LogInformation("Processing goods received for quality check: {ReceiptId}", @event.ReceiptId);

        foreach (ProcurementIntegrationEvents.GoodsReceivedItem item in @event.Items)
        {
            // Find mandatory quality points for this material and operation
            List<QualityPoint> qualityPoints = await qualityPointRepository.GetPointsForMaterial(item.MaterialId, "RECEIPT");

            foreach (QualityPoint point in qualityPoints)
            {
                Guid checkId = Guid.NewGuid();
                QualityCheck check = QualityCheck.Create(
                    checkId,
                    "TENANT-001", // Ideally get from event, but Procurement events currently lack it
                    point.Id,
                    @event.ReceiptId.ToString(),
                    "INVENTORY_RECEIPT",
                    item.MaterialId);

                await eventStore.SaveAggregateAsync(check);

                logger.LogInformation(
                    "Created IQC {CheckId} for Material {MaterialId} from Receipt {ReceiptId}",
                    checkId, item.MaterialId, @event.ReceiptId);
            }
        }
    }

    /// <summary>
    /// Handle production order start - creates mandatory PQC (Process Quality Control)
    /// </summary>
    public async Task Handle(ProductionIntegrationEvents.ProductionOrderReleasedIntegrationEvent @event, CancellationToken ct)
    {
        logger.LogInformation("Processing production order for quality check: {OrderId}", @event.OrderId);

        List<QualityPoint> qualityPoints = await qualityPointRepository.GetPointsForMaterial(@event.MaterialId, "PRODUCTION_START");

        foreach (QualityPoint point in qualityPoints)
        {
            Guid checkId = Guid.NewGuid();
            QualityCheck check = QualityCheck.Create(
                checkId,
                @event.TenantId,
                point.Id,
                @event.OrderId.ToString(),
                "PRODUCTION_ORDER",
                @event.MaterialId);

            await eventStore.SaveAggregateAsync(check);

            logger.LogInformation(
                "Created PQC {CheckId} for Production Order {OrderId}",
                checkId, @event.OrderId);
        }
    }
}

public interface IQualityPointRepository
{
    Task<List<QualityPoint>> GetPointsForMaterial(string materialId, string operationType);
}
