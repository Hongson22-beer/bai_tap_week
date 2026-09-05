using Microsoft.EntityFrameworkCore;
using ThueXe.Api.Data;
using ThueXe.Api.Dtos;
using ThueXe.Api.Models;

namespace ThueXe.Api.Services;

public interface ILoaiXeService
{
    Task<IEnumerable<LoaiXeResponseDto>> GetAllAsync();
    Task<LoaiXeResponseDto?> GetByIdAsync(int id);
    Task<LoaiXeResponseDto> CreateAsync(CreateLoaiXeDto dto);
    Task<bool> UpdateAsync(int id, UpdateLoaiXeDto dto);
    Task<(bool Success, string? ErrorMessage)> DeleteAsync(int id);
}

public class LoaiXeService : ILoaiXeService
{
    private readonly AppDbContext _context;

    public LoaiXeService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<LoaiXeResponseDto>> GetAllAsync()
    {
        return await _context.LoaiXes
            .Select(l => new LoaiXeResponseDto(
                l.Id, l.MaLoai, l.TenLoai, l.SoCho, l.MoTa, l.Xes.Count
            ))
            .ToListAsync();
    }

    public async Task<LoaiXeResponseDto?> GetByIdAsync(int id)
    {
        var l = await _context.LoaiXes
            .Include(x => x.Xes)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (l == null) return null;

        return new LoaiXeResponseDto(l.Id, l.MaLoai, l.TenLoai, l.SoCho, l.MoTa, l.Xes.Count);
    }

    public async Task<LoaiXeResponseDto> CreateAsync(CreateLoaiXeDto dto)
    {
        if (dto.SoCho <= 0)
        {
            throw new ArgumentException("Số chỗ ngồi phải lớn hơn 0.");
        }

        var entity = new LoaiXe
        {
            MaLoai = dto.MaLoai.Trim(),
            TenLoai = dto.TenLoai.Trim(),
            SoCho = dto.SoCho,
            MoTa = dto.MoTa?.Trim()
        };

        _context.LoaiXes.Add(entity);
        await _context.SaveChangesAsync();

        return new LoaiXeResponseDto(entity.Id, entity.MaLoai, entity.TenLoai, entity.SoCho, entity.MoTa, 0);
    }

    public async Task<bool> UpdateAsync(int id, UpdateLoaiXeDto dto)
    {
        if (dto.SoCho <= 0)
        {
            throw new ArgumentException("Số chỗ ngồi phải lớn hơn 0.");
        }

        var entity = await _context.LoaiXes.FindAsync(id);
        if (entity == null) return false;

        entity.TenLoai = dto.TenLoai.Trim();
        entity.SoCho = dto.SoCho;
        entity.MoTa = dto.MoTa?.Trim();

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<(bool Success, string? ErrorMessage)> DeleteAsync(int id)
    {
        var entity = await _context.LoaiXes
            .Include(l => l.Xes)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (entity == null) return (false, "NotFound");

        if (entity.Xes.Any())
        {
            return (false, "Conflict: Không thể xóa loại xe này vì vẫn còn xe thuộc loại xe này.");
        }

        _context.LoaiXes.Remove(entity);
        await _context.SaveChangesAsync();
        return (true, null);
    }
}