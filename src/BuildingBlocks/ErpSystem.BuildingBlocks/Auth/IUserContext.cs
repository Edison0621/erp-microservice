namespace ErpSystem.BuildingBlocks.Auth;

public interface IUserContext
{
    bool IsAuthenticated { get; }
    Guid UserId { get; }
    string? TenantId { get; }
    string? Email { get; }
    string? Name { get; }
    List<string> Roles { get; }
}
