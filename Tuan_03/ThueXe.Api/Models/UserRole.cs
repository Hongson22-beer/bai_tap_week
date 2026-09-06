namespace ThueXe.Api.Models;

public class UserRole
{
    public Guid UserId { get; set; }
    public AppUser? User { get; set; }

    public Guid RoleId { get; set; }
    public Role? Role { get; set; }
}