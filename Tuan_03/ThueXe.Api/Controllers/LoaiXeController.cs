using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThueXe.Api.Data;
using ThueXe.Api.Models;

namespace ThueXe.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LoaiXeController : ControllerBase
{
    private readonly AppDbContext _context;

    public LoaiXeController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll()
    {
        var list = await _context.LoaiXes
            .AsNoTracking()
            .Select(l => new LoaiXeResponseDto(l.Id, l.TenLoai))
            .ToListAsync();

        return Ok(list);
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id)
    {
        var loaiXe = await _context.LoaiXes.FindAsync(id);
        if (loaiXe == null)
        {
            return NotFound(new { message = $"Không tìm thấy loại xe với mã: {id}" });
        }

        return Ok(new LoaiXeResponseDto(loaiXe.Id, loaiXe.TenLoai));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Create([FromBody] LoaiXeRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.TenLoai))
        {
            return BadRequest(new { message = "Tên loại xe không được để trống." });
        }

        var loaiXe = new LoaiXe
        {
            TenLoai = dto.TenLoai.Trim()
        };

        _context.LoaiXes.Add(loaiXe);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { id = loaiXe.Id },
            new LoaiXeResponseDto(loaiXe.Id, loaiXe.TenLoai)
        );
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Update(int id, [FromBody] LoaiXeRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.TenLoai))
        {
            return BadRequest(new { message = "Tên loại xe không được để trống." });
        }

        var loaiXe = await _context.LoaiXes.FindAsync(id);
        if (loaiXe == null)
        {
            return NotFound(new { message = $"Không tìm thấy loại xe với mã: {id}" });
        }

        loaiXe.TenLoai = dto.TenLoai.Trim();
        await _context.SaveChangesAsync();

        return Ok(new { message = $"Cập nhật loại xe {id} thành công", data = new LoaiXeResponseDto(loaiXe.Id, loaiXe.TenLoai) });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Delete(int id)
    {
        var loaiXe = await _context.LoaiXes.FindAsync(id);
        if (loaiXe == null)
        {
            return NotFound(new { message = $"Không tìm thấy loại xe với mã: {id}" });
        }

        _context.LoaiXes.Remove(loaiXe);
        await _context.SaveChangesAsync();

        return Ok(new { message = $"Xóa loại xe {id} thành công" });
    }
}

public record LoaiXeRequestDto(string TenLoai);
public record LoaiXeResponseDto(int Id, string TenLoai);