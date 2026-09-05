using Microsoft.EntityFrameworkCore;
using ThueXe.Api.Data;
using ThueXe.Api.Dtos;
using ThueXe.Api.Models;

namespace ThueXe.Api.Services
{
    public interface IHangXeService
    {
        Task<IEnumerable<HangXeReadDto>> GetAllAsync();
        Task<HangXeReadDto?> GetByIdAsync(int id);
        Task<HangXeReadDto> CreateAsync(CreateHangXeDto dto);
        Task<bool> UpdateAsync(int id, UpdateHangXeDto dto);
        Task<bool> DeleteAsync(int id);
    }

    public class HangXeService : IHangXeService
    {
        private readonly AppDbContext _context;

        public HangXeService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<HangXeReadDto>> GetAllAsync()
        {
            return await _context.HangXes
                .Select(h => new HangXeReadDto
                {
                    Id = h.Id,
                    TenHang = h.TenHang,
                    QuocGia = h.QuocGia,
                    MoTa = h.MoTa
                }).ToListAsync();
        }

        public async Task<HangXeReadDto?> GetByIdAsync(int id)
        {
            var entity = await _context.HangXes.FindAsync(id);
            if (entity == null) return null;

            return new HangXeReadDto
            {
                Id = entity.Id,
                TenHang = entity.TenHang,
                QuocGia = entity.QuocGia,
                MoTa = entity.MoTa
            };
        }

        public async Task<HangXeReadDto> CreateAsync(CreateHangXeDto dto)
        {
            // Kiểm tra trùng tên hãng xe
            bool isDuplicate = await _context.HangXes
                .AnyAsync(h => h.TenHang.ToLower() == dto.TenHang.Trim().ToLower());

            if (isDuplicate)
            {
                throw new InvalidOperationException($"Hãng xe '{dto.TenHang}' đã tồn tại.");
            }

            var entity = new HangXe
            {
                TenHang = dto.TenHang.Trim(),
                QuocGia = dto.QuocGia?.Trim(),
                MoTa = dto.MoTa?.Trim()
            };

            _context.HangXes.Add(entity);
            await _context.SaveChangesAsync();

            return new HangXeReadDto
            {
                Id = entity.Id,
                TenHang = entity.TenHang,
                QuocGia = entity.QuocGia,
                MoTa = entity.MoTa
            };
        }

        public async Task<bool> UpdateAsync(int id, UpdateHangXeDto dto)
        {
            var entity = await _context.HangXes.FindAsync(id);
            if (entity == null) return false;

            bool isDuplicate = await _context.HangXes
                .AnyAsync(h => h.Id != id && h.TenHang.ToLower() == dto.TenHang.Trim().ToLower());

            if (isDuplicate)
            {
                throw new InvalidOperationException($"Hãng xe '{dto.TenHang}' đã tồn tại.");
            }

            entity.TenHang = dto.TenHang.Trim();
            entity.QuocGia = dto.QuocGia?.Trim();
            entity.MoTa = dto.MoTa?.Trim();

            await _context.SaveChangesAsync();
            return true;
        }

       public async Task<bool> DeleteAsync(int id)
{
    var entity = await _context.HangXes.FindAsync(id);
    if (entity == null) return false;

    // Kiểm tra trực tiếp bảng Xe xem có xe nào thuộc hãng này không
    bool hasXe = await _context.Xes.AnyAsync(x => x.IdHangXe == id);
    if (hasXe)
    {
        throw new InvalidOperationException("Không thể xóa hãng xe này vì vẫn còn xe thuộc hãng.");
    }

    _context.HangXes.Remove(entity);
    await _context.SaveChangesAsync();
    return true;
}
    }
}