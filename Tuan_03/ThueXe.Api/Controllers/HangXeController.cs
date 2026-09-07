using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThueXe.Api.Data;
using ThueXe.Api.Models;

namespace ThueXe.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class HangXeController : ControllerBase
{
    private readonly AppDbContext _context;

    public HangXeController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll()
    {
        var list = await _context.HangXes
            .AsNoTracking()
            .Select(h => new HangXeResponseDto(h.Id, h.TenHang))
            .ToListAsync();

        return Ok(list);
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id)
    {
        var hangXe = await _context.HangXes.FindAsync(id);
        if (hangXe == null)
        {
            return NotFound(new { message = $"Không tìm thấy hãng xe với mã: {id}" });
        }

        return Ok(new HangXeResponseDto(hangXe.Id, hangXe.TenHang));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Create([FromBody] HangXeRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.TenHang))
        {
            return BadRequest(new { message = "Tên hãng xe không được để trống." });
        }

        var hangXe = new HangXe
        {
            TenHang = dto.TenHang.Trim()
        };

        _context.HangXes.Add(hangXe);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { id = hangXe.Id },
            new HangXeResponseDto(hangXe.Id, hangXe.TenHang)
        );
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Update(int id, [FromBody] HangXeRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.TenHang))
        {
            return BadRequest(new { message = "Tên hãng xe không được để trống." });
        }

        var hangXe = await _context.HangXes.FindAsync(id);
        if (hangXe == null)
        {
            return NotFound(new { message = $"Không tìm thấy hãng xe với mã: {id}" });
        }

        hangXe.TenHang = dto.TenHang.Trim();
        await _context.SaveChangesAsync();

        return Ok(new { message = $"Cập nhật hãng xe {id} thành công", data = new HangXeResponseDto(hangXe.Id, hangXe.TenHang) });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Delete(int id)
    {
        var hangXe = await _context.HangXes.FindAsync(id);
        if (hangXe == null)
        {
            return NotFound(new { message = $"Không tìm thấy hãng xe với mã: {id}" });
        }

        _context.HangXes.Remove(hangXe);
        await _context.SaveChangesAsync();

        return Ok(new { message = $"Xóa hãng xe {id} thành công" });
    }
}

public record HangXeRequestDto(string TenHang);
public record HangXeResponseDto(int Id, string TenHang);