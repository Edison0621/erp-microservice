using System.Text.Json;
using ErpSystem.MasterData.Domain;
using MediatR;

namespace ErpSystem.MasterData.Infrastructure;

public class MaterialProjection : 
    INotificationHandler<MaterialCreatedEvent>,
    INotificationHandler<MaterialInfoUpdatedEvent>,
    INotificationHandler<MaterialCostChangedEvent>,
    INotificationHandler<MaterialAttributesUpdatedEvent>,
    INotificationHandler<MaterialStatusChangedEvent>
{
    private readonly MasterDataReadDbContext _dbContext;

    public MaterialProjection(MasterDataReadDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(MaterialCreatedEvent notification, CancellationToken cancellationToken)
    {
        var material = new MaterialReadModel
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
        _dbContext.Materials.Add(material);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task Handle(MaterialInfoUpdatedEvent notification, CancellationToken cancellationToken)
    {
        var material = await _dbContext.Materials.FindAsync(new object[] { notification.MaterialId }, cancellationToken);
        if (material != null)
        {
            material.MaterialName = notification.MaterialName;
            material.Manufacturer = notification.Manufacturer;
            material.Specification = notification.Specification;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task Handle(MaterialCostChangedEvent notification, CancellationToken cancellationToken)
    {
        var material = await _dbContext.Materials.FindAsync(new object[] { notification.MaterialId }, cancellationToken);
        if (material != null)
        {
            material.TotalCost = notification.NewCost.Total;
            material.CostDetail = JsonSerializer.Serialize(notification.NewCost);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task Handle(MaterialAttributesUpdatedEvent notification, CancellationToken cancellationToken)
    {
        var material = await _dbContext.Materials.FindAsync(new object[] { notification.MaterialId }, cancellationToken);
        if (material != null)
        {
            material.Attributes = JsonSerializer.Serialize(notification.Attributes);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task Handle(MaterialStatusChangedEvent notification, CancellationToken cancellationToken)
    {
        var material = await _dbContext.Materials.FindAsync(new object[] { notification.MaterialId }, cancellationToken);
        if (material != null)
        {
            material.IsActive = notification.IsActive;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
