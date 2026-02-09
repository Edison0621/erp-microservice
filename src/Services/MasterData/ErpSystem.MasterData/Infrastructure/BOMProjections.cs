using MediatR;
using ErpSystem.MasterData.Domain;
using System.Text.Json;

namespace ErpSystem.MasterData.Infrastructure;

public class BomProjections(MasterDataReadDbContext context) :
    INotificationHandler<BomCreatedEvent>,
    INotificationHandler<BomComponentAddedEvent>,
    INotificationHandler<BomStatusChangedEvent>
{
    public async Task Handle(BomCreatedEvent e, CancellationToken ct)
    {
        BomReadModel model = new BomReadModel
        {
            BomId = e.BomId,
            ParentMaterialId = e.ParentMaterialId,
            BomName = e.BomName,
            Version = e.Version,
            EffectiveDate = e.EffectiveDate,
            Status = nameof(BomStatus.Draft),
            Components = "[]"
        };
        context.BoMs.Add(model);
        await context.SaveChangesAsync(ct);
    }

    public async Task Handle(BomComponentAddedEvent e, CancellationToken ct)
    {
        BomReadModel? model = await context.BoMs.FindAsync([e.BomId], ct);
        if (model != null)
        {
            List<BomComponent> components = JsonSerializer.Deserialize<List<BomComponent>>(model.Components) ?? [];
            components.Add(new BomComponent(e.MaterialId, e.Quantity, e.Note));
            model.Components = JsonSerializer.Serialize(components);
            await context.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(BomStatusChangedEvent e, CancellationToken ct)
    {
        BomReadModel? model = await context.BoMs.FindAsync([e.BomId], ct);
        if (model != null)
        {
            model.Status = e.Status.ToString();
            await context.SaveChangesAsync(ct);
        }
    }
}
