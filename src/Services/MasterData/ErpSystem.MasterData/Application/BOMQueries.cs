using Microsoft.EntityFrameworkCore;
using ErpSystem.MasterData.Infrastructure;

namespace ErpSystem.MasterData.Application;

public class BOMQueries
{
    private readonly MasterDataReadDbContext _context;

    public BOMQueries(MasterDataReadDbContext context)
    {
        _context = context;
    }

    public async Task<List<BOMReadModel>> GetAllBOMs()
    {
        return await _context.BOMs.ToListAsync();
    }

    public async Task<BOMReadModel?> GetBOMById(Guid id)
    {
        return await _context.BOMs.FindAsync(id);
    }

    public async Task<List<BOMReadModel>> GetBOMsByParentMaterial(Guid parentMaterialId)
    {
        return await _context.BOMs.Where(b => b.ParentMaterialId == parentMaterialId).ToListAsync();
    }
}
