using Microsoft.EntityFrameworkCore;
using ErpSystem.MasterData.Infrastructure;

namespace ErpSystem.MasterData.Application;

/// <summary>
/// Class BOMQueries.
/// </summary>
/// <param name="context">The context.</param>
public class BomQueries(MasterDataReadDbContext context)
{
    /// <summary>
    /// Gets all bo ms.
    /// </summary>
    /// <returns>List{BOMReadModel}.</returns>
    public async Task<List<BomReadModel>> GetAllBoMs()
    {
        return await context.BoMs.ToListAsync();
    }

    /// <summary>
    /// Gets the bom by identifier.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <returns>System.Nullable{BOMReadModel}.</returns>
    public async Task<BomReadModel?> GetBomById(Guid id)
    {
        return await context.BoMs.FindAsync(id);
    }

    /// <summary>
    /// Gets the bo ms by parent material.
    /// </summary>
    /// <param name="parentMaterialId">The parent material identifier.</param>
    /// <returns>List{BOMReadModel}.</returns>
    public async Task<List<BomReadModel>> GetBoMsByParentMaterial(Guid parentMaterialId)
    {
        return await context.BoMs.Where(b => b.ParentMaterialId == parentMaterialId).ToListAsync();
    }
}
