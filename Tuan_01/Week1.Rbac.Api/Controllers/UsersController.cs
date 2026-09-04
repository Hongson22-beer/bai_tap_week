using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Week1.Rbac.Api.Contracts;
using Week1.Rbac.Api.Data;
using Week1.Rbac.Api.Models;

namespace Week1.Rbac.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserResponse>>> GetAll(CancellationToken ct)
    {
        var users = await db.Users
            .AsNoTracking()
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .Select(u => new UserResponse(
                u.Id,
                u.Email,
                u.DisplayName,
                u.IsActive,
                u.CreatedAt,
                u.UserRoles.Select(ur => ur.Role.Name).ToList()
            ))
            .ToListAsync(ct);

        return Ok(users);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserResponse>> GetById(Guid id, CancellationToken ct)
    {
        var user = await db.Users
            .AsNoTracking()
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .Where(u => u.Id == id)
            .Select(u => new UserResponse(
                u.Id,
                u.Email,
                u.DisplayName,
                u.IsActive,
                u.CreatedAt,
                u.UserRoles.Select(ur => ur.Role.Name).ToList()
            ))
            .SingleOrDefaultAsync(ct);

        return user is null ? NotFound() : Ok(user);
    }

    [HttpPost]
    public async Task<ActionResult<UserResponse>> Create([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == email, ct))
        {
            return Conflict(new ProblemDetails { Title = "Conflict", Detail = "Email already exists." });
        }

        var user = new User
        {
            Email = email,
            DisplayName = request.DisplayName.Trim()
        };

        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, request.Password);

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        var response = new UserResponse(
            user.Id,
            user.Email,
            user.DisplayName,
            user.IsActive,
            user.CreatedAt,
            []
        );

        return CreatedAtAction(nameof(GetById), new { id = user.Id }, response);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest request, CancellationToken ct)
    {
        var user = await db.Users.FindAsync([id], ct);
        if (user is null) return NotFound();

        user.DisplayName = request.DisplayName.Trim();
        user.IsActive = request.IsActive;

        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var user = await db.Users.FindAsync([id], ct);
        if (user is null) return NotFound();

        db.Users.Remove(user);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // Gán Role cho User: PUT /api/users/{userId}/roles/{roleId}
    [HttpPut("{userId:guid}/roles/{roleId:guid}")]
    public async Task<IActionResult> AssignRole(Guid userId, Guid roleId, CancellationToken ct)
    {
        var userExists = await db.Users.AnyAsync(u => u.Id == userId, ct);
        var roleExists = await db.Roles.AnyAsync(r => r.Id == roleId, ct);

        if (!userExists || !roleExists) return NotFound();

        var alreadyAssigned = await db.UserRoles.AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId, ct);
        if (alreadyAssigned) return NoContent();

        db.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleId });
        await db.SaveChangesAsync(ct);

        return NoContent();
    }

    // Gỡ Role khỏi User: DELETE /api/users/{userId}/roles/{roleId}
    [HttpDelete("{userId:guid}/roles/{roleId:guid}")]
    public async Task<IActionResult> RemoveRole(Guid userId, Guid roleId, CancellationToken ct)
    {
        var userRole = await db.UserRoles.FindAsync([userId, roleId], ct);
        if (userRole is null) return NotFound();

        db.UserRoles.Remove(userRole);
        await db.SaveChangesAsync(ct);

        return NoContent();
    }
}