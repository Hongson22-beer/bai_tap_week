using System.Security.Claims;
using BtlThueXe.Core.DTOs.Admin;
using BtlThueXe.Infrastructure;
using BtlThueXe.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BtlThueXe.Api.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = "ADMIN")]
public class AdminUsersController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AdminUsersController(ApplicationDbContext context) => _context = context;

    private int GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue("sub")
                    ?? User.FindFirstValue("id");
        if (!int.TryParse(value, out var id))
            throw new UnauthorizedAccessException("Không xác định được người dùng từ JWT.");
        return id;
    }

    [HttpGet]
    public async Task<ActionResult<List<AdminUserResponse>>> GetAll()
    {
        var users = await _context.NguoiDungs
            .AsNoTracking()
            .Include(x => x.IdVaiTros)
            .OrderBy(x => x.Id)
            .ToListAsync();

        return Ok(users.Select(x => new AdminUserResponse
        {
            Id = x.Id,
            Email = x.Email,
            HoTen = x.HoTen,
            SoDienThoai = x.SoDienThoai,
            DangHoatDong = x.DangHoatDong,
            Roles = x.IdVaiTros.Select(r => r.Ten).ToList()
        }).ToList());
    }

    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult> UpdateStatus(int id, UpdateUserStatusRequest request)
    {
        var user = await _context.NguoiDungs.FindAsync(id);
        if (user == null) return NotFound(new { message = "Không tìm thấy người dùng." });

        user.DangHoatDong = request.DangHoatDong;
        user.UpdatedAt = DateTime.Now;
        _context.AuditLogs.Add(new AuditLog
        {
            IdNguoiDung = GetCurrentUserId(),
            HanhDong = "ADMIN_USER_STATUS_CHANGED",
            LoaiDoiTuong = "USER",
            IdDoiTuong = id,
            MoTa = $"Đổi trạng thái hoạt động người dùng #{id} thành {request.DangHoatDong}.",
            ThoiGian = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
        return Ok(new { message = "Cập nhật trạng thái tài khoản thành công." });
    }

    [HttpPut("{id:int}/roles")]
    public async Task<ActionResult> UpdateRoles(int id, UpdateUserRolesRequest request)
    {
        var user = await _context.NguoiDungs
            .Include(x => x.IdVaiTros)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (user == null) return NotFound(new { message = "Không tìm thấy người dùng." });

        var requested = request.Roles
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim().ToUpperInvariant())
            .Distinct()
            .ToList();
        if (requested.Count == 0)
            return BadRequest(new { message = "Phải có ít nhất một vai trò." });

        var roles = await _context.VaiTros.Where(x => requested.Contains(x.Ten)).ToListAsync();
        if (roles.Count != requested.Count)
            return BadRequest(new { message = "Có vai trò không tồn tại trong hệ thống." });

        user.IdVaiTros.Clear();
        foreach (var role in roles) user.IdVaiTros.Add(role);
        user.UpdatedAt = DateTime.Now;

        _context.AuditLogs.Add(new AuditLog
        {
            IdNguoiDung = GetCurrentUserId(),
            HanhDong = "ADMIN_USER_ROLES_CHANGED",
            LoaiDoiTuong = "USER",
            IdDoiTuong = id,
            MoTa = $"Cập nhật vai trò người dùng #{id}: {string.Join(",", requested)}.",
            ThoiGian = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
        return Ok(new { message = "Cập nhật vai trò thành công." });
    }
}
