using BtlThueXe.Core.DTOs.Vehicles;
using BtlThueXe.Core.Interfaces;
using BtlThueXe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BtlThueXe.Infrastructure.Services;

public class CategoryService : ICategoryService
{
    private readonly ApplicationDbContext _context;

    public CategoryService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<BrandDto>> GetBrandsAsync()
    {
        return await _context.HangXes
            .Select(b => new BrandDto
            {
                Id = b.Id,
                Ten = b.Ten,
                QuocGia = b.QuocGia
            })
            .ToListAsync();
    }

    public async Task<BrandDto> CreateBrandAsync(CreateBrandDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Ten))
            throw new Exception("Tên hãng xe không được để trống.");

        if (await _context.HangXes.AnyAsync(b => b.Ten.ToLower() == dto.Ten.Trim().ToLower()))
            throw new Exception("Hãng xe này đã tồn tại.");

        var brand = new HangXe
        {
            Ten = dto.Ten.Trim(),
            QuocGia = dto.QuocGia?.Trim()
        };

        _context.HangXes.Add(brand);
        await _context.SaveChangesAsync();

        return new BrandDto
        {
            Id = brand.Id,
            Ten = brand.Ten,
            QuocGia = brand.QuocGia
        };
    }

    public async Task<List<VehicleTypeDto>> GetVehicleTypesAsync()
    {
        return await _context.LoaiXes
            .Select(t => new VehicleTypeDto
            {
                Id = t.Id,
                Ten = t.Ten,
                SoCho = t.SoCho,
                MoTa = t.MoTa
            })
            .ToListAsync();
    }

    public async Task<VehicleTypeDto> CreateVehicleTypeAsync(CreateVehicleTypeDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Ten))
            throw new Exception("Tên loại xe không được để trống.");

        if (dto.SoCho <= 0)
            throw new Exception("Số chỗ ngồi phải lớn hơn 0.");

        if (await _context.LoaiXes.AnyAsync(t => t.Ten.ToLower() == dto.Ten.Trim().ToLower()))
            throw new Exception("Loại xe này đã tồn tại.");

        var type = new LoaiXe
        {
            Ten = dto.Ten.Trim(),
            SoCho = dto.SoCho,
            MoTa = dto.MoTa?.Trim()
        };

        _context.LoaiXes.Add(type);
        await _context.SaveChangesAsync();

        return new VehicleTypeDto
        {
            Id = type.Id,
            Ten = type.Ten,
            SoCho = type.SoCho,
            MoTa = type.MoTa
        };
    }
}