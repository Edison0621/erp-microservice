using ErpSystem.BuildingBlocks.Domain;

namespace ErpSystem.MasterData.Domain;

// --- Value Objects ---

public record BOMComponent(Guid MaterialId, decimal Quantity, string? Note);

// --- Domain Events ---

public record BOMCreatedEvent(
    Guid BOMId, 
    Guid ParentMaterialId, 
    string BOMName, 
    string Version,
    DateTime EffectiveDate
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record BOMComponentAddedEvent(
    Guid BOMId, 
    Guid MaterialId, 
    decimal Quantity, 
    string? Note
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record BOMStatusChangedEvent(
    Guid BOMId, 
    BOMStatus Status, 
    string? Reason
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

// --- Enums ---

public enum BOMStatus
{
    Draft = 1,
    Active,
    Obsolete
}

// --- Aggregate ---

public class BillOfMaterials : AggregateRoot<Guid>
{
    public Guid ParentMaterialId { get; private set; }
    public string BOMName { get; private set; } = string.Empty;
    public string Version { get; private set; } = string.Empty;
    public BOMStatus Status { get; private set; }
    public DateTime EffectiveDate { get; private set; }

    private readonly List<BOMComponent> _components = new();
    public IReadOnlyCollection<BOMComponent> Components => _components.AsReadOnly();

    public static BillOfMaterials Create(
        Guid id, 
        Guid parentMaterialId, 
        string name, 
        string version, 
        DateTime effectiveDate)
    {
        var bom = new BillOfMaterials();
        bom.ApplyChange(new BOMCreatedEvent(id, parentMaterialId, name, version, effectiveDate));
        return bom;
    }

    public void AddComponent(Guid materialId, decimal quantity, string? note)
    {
        if (Status != BOMStatus.Draft)
            throw new InvalidOperationException("Components can only be added to a Draft BOM.");
        
        if (materialId == ParentMaterialId)
            throw new InvalidOperationException("A material cannot be a component of its own BOM.");

        ApplyChange(new BOMComponentAddedEvent(Id, materialId, quantity, note));
    }

    public void Activate()
    {
        if (Status == BOMStatus.Active) return;
        if (!_components.Any())
            throw new InvalidOperationException("Cannot activate an empty BOM.");

        ApplyChange(new BOMStatusChangedEvent(Id, BOMStatus.Active, "Manual Activation"));
    }

    public void Deactivate(string reason)
    {
        if (Status == BOMStatus.Obsolete) return;
        ApplyChange(new BOMStatusChangedEvent(Id, BOMStatus.Obsolete, reason));
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case BOMCreatedEvent e:
                Id = e.BOMId;
                ParentMaterialId = e.ParentMaterialId;
                BOMName = e.BOMName;
                Version = e.Version;
                EffectiveDate = e.EffectiveDate;
                Status = BOMStatus.Draft;
                break;
            case BOMComponentAddedEvent e:
                _components.Add(new BOMComponent(e.MaterialId, e.Quantity, e.Note));
                break;
            case BOMStatusChangedEvent e:
                Status = e.Status;
                break;
        }
    }
}
