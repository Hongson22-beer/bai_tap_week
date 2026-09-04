using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Week1.Rbac.Api.Contracts;
using Week1.Rbac.Api.Data;
using Week1.Rbac.Api.Models;

namespace Week1.Rbac.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RolesController : ControllerBase
{
    private readonly AppDbContext _context;

    public RolesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RoleResponse>>> GetRoles()
    {
        var roles = await _context.Roles
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .Select(r => new RoleResponse(
                r.Id,
                r.Name,
                r.Description,
                r.RolePermissions.Select(rp => rp.Permission.Code).ToList()
            ))
            .ToListAsync();

        return Ok(roles);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RoleResponse>> GetRole(Guid id)
    {
        var role = await _context.Roles
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (role == null) return NotFound();

        var permissions = role.RolePermissions.Select(rp => rp.Permission.Code).ToList();
        return Ok(new RoleResponse(role.Id, role.Name, role.Description, permissions));
    }

    [HttpPost]
    public async Task<ActionResult<RoleResponse>> CreateRole(CreateRoleRequest request)
    {
        var role = new Role
        {
            Name = request.Name.Trim(),
            Description = request.Description.Trim()
        };

        _context.Roles.Add(role);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetRole), new { id = role.Id }, 
            new RoleResponse(role.Id, role.Name, role.Description, Array.Empty<string>()));
    }

    [HttpPut("{roleId:guid}/permissions/{permissionId:guid}")]
    public async Task<IActionResult> AssignPermission(Guid roleId, Guid permissionId)
    {
        var roleExists = await _context.Roles.AnyAsync(r => r.Id == roleId);
        var permExists = await _context.Permissions.AnyAsync(p => p.Id == permissionId);

        if (!roleExists || !permExists) return NotFound();

        var exists = await _context.RolePermissions
            .AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);

        if (!exists)
        {
            _context.RolePermissions.Add(new RolePermission 
            { 
                RoleId = roleId, 
                PermissionId = permissionId 
            });
            await _context.SaveChangesAsync();
        }

        return NoContent();
    }

    [HttpDelete("{roleId:guid}/permissions/{permissionId:guid}")]
    public async Task<IActionResult> RemovePermission(Guid roleId, Guid permissionId)
    {
        var rp = await _context.RolePermissions
            .FirstOrDefaultAsync(x => x.RoleId == roleId && x.PermissionId == permissionId);

        if (rp != null)
        {
            _context.RolePermissions.Remove(rp);
            await _context.SaveChangesAsync();
        }

        return NoContent();
    }
}