using MediatR;
using Microsoft.EntityFrameworkCore;
using ErpSystem.MasterData.Domain;
using System.Text.Json;

namespace ErpSystem.MasterData.Infrastructure;

public class BOMProjections : 
    INotificationHandler<BOMCreatedEvent>,
    INotificationHandler<BOMComponentAddedEvent>,
    INotificationHandler<BOMStatusChangedEvent>
{
    private readonly MasterDataReadDbContext _context;

    public BOMProjections(MasterDataReadDbContext context)
    {
        _context = context;
    }

    public async Task Handle(BOMCreatedEvent e, CancellationToken ct)
    {
        var model = new BOMReadModel
        {
            BOMId = e.BOMId,
            ParentMaterialId = e.ParentMaterialId,
            BOMName = e.BOMName,
            Version = e.Version,
            EffectiveDate = e.EffectiveDate,
            Status = BOMStatus.Draft.ToString(),
            Components = "[]"
        };
        _context.BOMs.Add(model);
        await _context.SaveChangesAsync(ct);
    }

    public async Task Handle(BOMComponentAddedEvent e, CancellationToken ct)
    {
        var model = await _context.BOMs.FindAsync(new object[] { e.BOMId }, ct);
        if (model != null)
        {
            var components = JsonSerializer.Deserialize<List<BOMComponent>>(model.Components) ?? new();
            components.Add(new BOMComponent(e.MaterialId, e.Quantity, e.Note));
            model.Components = JsonSerializer.Serialize(components);
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(BOMStatusChangedEvent e, CancellationToken ct)
    {
        var model = await _context.BOMs.FindAsync(new object[] { e.BOMId }, ct);
        if (model != null)
        {
            model.Status = e.Status.ToString();
            await _context.SaveChangesAsync(ct);
        }
    }
}
