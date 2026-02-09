using ErpSystem.BuildingBlocks.Domain;

namespace ErpSystem.MasterData.Domain;

// --- Events ---

public record CategoryCreatedEvent(
    Guid CategoryId, 
    string Code, 
    string Name, 
    Guid? ParentId, 
    int Level
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record CategoryMovedEvent(Guid CategoryId, Guid? NewParentId, int NewLevel) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

// --- Aggregate ---

public class MaterialCategory : AggregateRoot<Guid>
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public Guid? ParentId { get; private set; }
    public int Level { get; private set; }

    public static MaterialCategory Create(Guid id, string code, string name, Guid? parentId, int level)
    {
        if (level > 5) throw new ArgumentException("Category depth cannot exceed 5 levels");
        
        MaterialCategory category = new MaterialCategory();
        category.ApplyChange(new CategoryCreatedEvent(id, code, name, parentId, level));
        return category;
    }

    public void Move(Guid? newParentId, int newLevel)
    {
        if (newLevel > 5) throw new ArgumentException("Category depth cannot exceed 5 levels");
        this.ApplyChange(new CategoryMovedEvent(this.Id, newParentId, newLevel));
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case CategoryCreatedEvent e:
                this.Id = e.CategoryId;
                this.Code = e.Code;
                this.Name = e.Name;
                this.ParentId = e.ParentId;
                this.Level = e.Level;
                break;
            case CategoryMovedEvent e:
                this.ParentId = e.NewParentId;
                this.Level = e.NewLevel;
                break;
        }
    }
}
