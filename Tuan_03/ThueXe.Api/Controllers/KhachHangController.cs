using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThueXe.Api.Data;

namespace ThueXe.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class KhachHangController : ControllerBase
{
    private readonly AppDbContext _context;

    public KhachHangController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        // 1. Kiểm tra quyền BOLA/IDOR (Customer chỉ xem chính mình, Admin/Staff xem tất cả)
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var currentUserRole = User.FindFirstValue(ClaimTypes.Role);

        if (currentUserRole == "Customer" && currentUserId != id.ToString())
        {
            return Forbid();
        }

        // 2. Tìm user trong cơ sở dữ liệu thật
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound(new { message = $"Không tìm thấy khách hàng với mã: {id}" });
        }

        // 3. Trả về đúng thông tin từ database (FullName, Email)
        return Ok(new
        {
            id = user.Id,
            fullName = user.FullName,
            email = user.Email
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateKhachHangDto dto)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var currentUserRole = User.FindFirstValue(ClaimTypes.Role);

        if (currentUserRole == "Customer" && currentUserId != id.ToString())
        {
            return Forbid();
        }

        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound(new { message = $"Không tìm thấy khách hàng với mã: {id}" });
        }

        user.FullName = dto.FullName.Trim();
        await _context.SaveChangesAsync();

        return Ok(new 
        { 
            message = "Cập nhật thông tin thành công", 
            data = new { user.Id, user.FullName, user.Email } 
        });
    }
}

public record UpdateKhachHangDto(string FullName);