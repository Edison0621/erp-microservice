using ErpSystem.BuildingBlocks.Domain;

namespace ErpSystem.MasterData.Domain;

// --- Value Objects ---

public record BomComponent(Guid MaterialId, decimal Quantity, string? Note);

// --- Domain Events ---

public record BomCreatedEvent(
    Guid BomId, 
    Guid ParentMaterialId, 
    string BomName, 
    string Version,
    DateTime EffectiveDate
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record BomComponentAddedEvent(
    Guid BomId, 
    Guid MaterialId, 
    decimal Quantity, 
    string? Note
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record BomStatusChangedEvent(
    Guid BomId, 
    BomStatus Status, 
    string? Reason
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

// --- Enums ---

public enum BomStatus
{
    Draft = 1,
    Active,
    Obsolete
}

// --- Aggregate ---

public class BillOfMaterials : AggregateRoot<Guid>
{
    public Guid ParentMaterialId { get; private set; }
    public string BomName { get; private set; } = string.Empty;
    public string Version { get; private set; } = string.Empty;
    public BomStatus Status { get; private set; }
    public DateTime EffectiveDate { get; private set; }

    private readonly List<BomComponent> _components = [];
    public IReadOnlyCollection<BomComponent> Components => this._components.AsReadOnly();

    public static BillOfMaterials Create(
        Guid id, 
        Guid parentMaterialId, 
        string name, 
        string version, 
        DateTime effectiveDate)
    {
        BillOfMaterials bom = new();
        bom.ApplyChange(new BomCreatedEvent(id, parentMaterialId, name, version, effectiveDate));
        return bom;
    }

    public void AddComponent(Guid materialId, decimal quantity, string? note)
    {
        if (this.Status != BomStatus.Draft)
            throw new InvalidOperationException("Components can only be added to a Draft BOM.");
        
        if (materialId == this.ParentMaterialId)
            throw new InvalidOperationException("A material cannot be a component of its own BOM.");

        this.ApplyChange(new BomComponentAddedEvent(this.Id, materialId, quantity, note));
    }

    public void Activate()
    {
        if (this.Status == BomStatus.Active) return;
        if (!this._components.Any())
            throw new InvalidOperationException("Cannot activate an empty BOM.");

        this.ApplyChange(new BomStatusChangedEvent(this.Id, BomStatus.Active, "Manual Activation"));
    }

    public void Deactivate(string reason)
    {
        if (this.Status == BomStatus.Obsolete) return;
        this.ApplyChange(new BomStatusChangedEvent(this.Id, BomStatus.Obsolete, reason));
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case BomCreatedEvent e:
                this.Id = e.BomId;
                this.ParentMaterialId = e.ParentMaterialId;
                this.BomName = e.BomName;
                this.Version = e.Version;
                this.EffectiveDate = e.EffectiveDate;
                this.Status = BomStatus.Draft;
                break;
            case BomComponentAddedEvent e:
                this._components.Add(new BomComponent(e.MaterialId, e.Quantity, e.Note));
                break;
            case BomStatusChangedEvent e:
                this.Status = e.Status;
                break;
        }
    }
}
