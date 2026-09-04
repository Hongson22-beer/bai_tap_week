namespace Week1.Rbac.Api.Models;

public sealed class Permission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty; // Ví dụ: user.read, user.create
    public string Name { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}