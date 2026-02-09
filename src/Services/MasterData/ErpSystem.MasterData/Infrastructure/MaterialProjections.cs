using System.Text.Json;
using ErpSystem.MasterData.Domain;
using MediatR;

namespace ErpSystem.MasterData.Infrastructure;

public class MaterialProjection(MasterDataReadDbContext dbContext) :
    INotificationHandler<MaterialCreatedEvent>,
    INotificationHandler<MaterialInfoUpdatedEvent>,
    INotificationHandler<MaterialCostChangedEvent>,
    INotificationHandler<MaterialAttributesUpdatedEvent>,
    INotificationHandler<MaterialStatusChangedEvent>
{
    public async Task Handle(MaterialCreatedEvent notification, CancellationToken cancellationToken)
    {
        MaterialReadModel material = new()
        {
            MaterialId = notification.MaterialId,
            MaterialCode = notification.MaterialCode,
            MaterialName = notification.MaterialName,
            MaterialType = notification.MaterialType.ToString(),
            UnitOfMeasure = notification.UnitOfMeasure,
            CategoryId = notification.CategoryId,
            TotalCost = notification.InitialCost.Total,
            CostDetail = JsonSerializer.Serialize(notification.InitialCost),
            IsActive = false
        };
        dbContext.Materials.Add(material);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task Handle(MaterialInfoUpdatedEvent notification, CancellationToken cancellationToken)
    {
        MaterialReadModel? material = await dbContext.Materials.FindAsync([notification.MaterialId], cancellationToken);
        if (material != null)
        {
            material.MaterialName = notification.MaterialName;
            material.Manufacturer = notification.Manufacturer;
            material.Specification = notification.Specification;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task Handle(MaterialCostChangedEvent notification, CancellationToken cancellationToken)
    {
        MaterialReadModel? material = await dbContext.Materials.FindAsync([notification.MaterialId], cancellationToken);
        if (material != null)
        {
            material.TotalCost = notification.NewCost.Total;
            material.CostDetail = JsonSerializer.Serialize(notification.NewCost);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task Handle(MaterialAttributesUpdatedEvent notification, CancellationToken cancellationToken)
    {
        MaterialReadModel? material = await dbContext.Materials.FindAsync([notification.MaterialId], cancellationToken);
        if (material != null)
        {
            material.Attributes = JsonSerializer.Serialize(notification.Attributes);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task Handle(MaterialStatusChangedEvent notification, CancellationToken cancellationToken)
    {
        MaterialReadModel? material = await dbContext.Materials.FindAsync([notification.MaterialId], cancellationToken);
        if (material != null)
        {
            material.IsActive = notification.IsActive;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
