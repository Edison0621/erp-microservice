using ErpSystem.BuildingBlocks.Domain;

namespace ErpSystem.MasterData.Domain;

// --- Value Objects ---

public record CostDetail(decimal Material, decimal Labor, decimal FixedOverhead, decimal VariableOverhead)
{
    public decimal Total => this.Material + this.Labor + this.FixedOverhead + this.VariableOverhead;
}

public record MaterialAttribute(string Name, string Value, string Type);

// --- Domain Events ---

public record MaterialCreatedEvent(
    Guid MaterialId, 
    string MaterialCode, 
    string MaterialName, 
    MaterialType MaterialType, 
    string UnitOfMeasure, 
    Guid CategoryId,
    CostDetail InitialCost
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record MaterialInfoUpdatedEvent(
    Guid MaterialId, 
    string MaterialName, 
    string Description, 
    string Specification, 
    string Brand,
    string Manufacturer
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record MaterialCostChangedEvent(
    Guid MaterialId, 
    CostDetail NewCost, 
    DateTime EffectiveDate, 
    string Reason
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record MaterialAttributesUpdatedEvent(
    Guid MaterialId, 
    List<MaterialAttribute> Attributes
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record MaterialStatusChangedEvent(Guid MaterialId, bool IsActive, string? Reason) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

// --- Enums ---

public enum MaterialType
{
    RawMaterial = 1,
    SemiFinished,
    Finished,
    Service,
    Tool,
    Consumable,
    SparePart
}

// --- Aggregate ---

public class Material : AggregateRoot<Guid>
{
    public string MaterialCode { get; private set; } = string.Empty;
    public string MaterialName { get; private set; } = string.Empty;
    public MaterialType MaterialType { get; private set; }
    public string UnitOfMeasure { get; private set; } = string.Empty;
    public Guid CategoryId { get; private set; }
    public bool IsActive { get; private set; }
    
    public CostDetail CurrentCost { get; private set; } = new(0, 0, 0, 0);
    public string Description { get; private set; } = string.Empty;
    public string Specification { get; private set; } = string.Empty;
    public string Brand { get; private set; } = string.Empty;
    public string Manufacturer { get; private set; } = string.Empty;

    private readonly List<MaterialAttribute> _attributes = [];
    public IReadOnlyCollection<MaterialAttribute> Attributes => this._attributes.AsReadOnly();

    public static Material Create(
        Guid id, 
        string code, 
        string name, 
        MaterialType type, 
        string uom, 
        Guid categoryId, 
        CostDetail initialCost)
    {
        Material material = new Material();
        material.ApplyChange(new MaterialCreatedEvent(id, code, name, type, uom, categoryId, initialCost));
        return material;
    }

    public void UpdateInfo(string name, string description, string specification, string brand, string manufacturer)
    {
        this.ApplyChange(new MaterialInfoUpdatedEvent(this.Id, name, description, specification, brand, manufacturer));
    }

    public void ChangeCost(CostDetail newCost, DateTime effectiveDate, string reason)
    {
        this.ApplyChange(new MaterialCostChangedEvent(this.Id, newCost, effectiveDate, reason));
    }

    public void UpdateAttributes(List<MaterialAttribute> attributes)
    {
        this.ApplyChange(new MaterialAttributesUpdatedEvent(this.Id, attributes));
    }

    public void Activate()
    {
        if (this.IsActive) return;
        // Logic check for completeness could go here
        this.ApplyChange(new MaterialStatusChangedEvent(this.Id, true, "Manual Activation"));
    }

    public void Deactivate(string reason)
    {
        if (!this.IsActive) return;
        this.ApplyChange(new MaterialStatusChangedEvent(this.Id, false, reason));
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case MaterialCreatedEvent e:
                this.Id = e.MaterialId;
                this.MaterialCode = e.MaterialCode;
                this.MaterialName = e.MaterialName;
                this.MaterialType = e.MaterialType;
                this.UnitOfMeasure = e.UnitOfMeasure;
                this.CategoryId = e.CategoryId;
                this.CurrentCost = e.InitialCost;
                this.IsActive = false;
                break;
            case MaterialInfoUpdatedEvent e:
                this.MaterialName = e.MaterialName;
                this.Description = e.Description;
                this.Specification = e.Specification;
                this.Brand = e.Brand;
                this.Manufacturer = e.Manufacturer;
                break;
            case MaterialCostChangedEvent e:
                this.CurrentCost = e.NewCost;
                break;
            case MaterialAttributesUpdatedEvent e:
                this._attributes.Clear();
                this._attributes.AddRange(e.Attributes);
                break;
            case MaterialStatusChangedEvent e:
                this.IsActive = e.IsActive;
                break;
        }
    }
}
