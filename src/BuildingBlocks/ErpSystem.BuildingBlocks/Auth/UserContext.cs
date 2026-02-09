using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace ErpSystem.BuildingBlocks.Auth;

public class UserContext(IHttpContextAccessor httpContextAccessor) : IUserContext
{
    public bool IsAuthenticated => httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public Guid UserId
    {
        get
        {
            Claim? userIdClaim = httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier) 
                                 ?? httpContextAccessor.HttpContext?.User?.FindFirst("sub");
            
            return userIdClaim is not null && Guid.TryParse(userIdClaim.Value, out Guid userId) 
                ? userId 
                : Guid.Empty;
        }
    }

    public string? TenantId => httpContextAccessor.HttpContext?.User?.FindFirst("tenant_id")?.Value;

    public string? Email => httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value;

    public string? Name => httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Name)?.Value;

    public List<string> Roles =>
        httpContextAccessor.HttpContext?.User?.FindAll(ClaimTypes.Role)
        .Select(c => c.Value)
        .ToList() ?? [];
}
