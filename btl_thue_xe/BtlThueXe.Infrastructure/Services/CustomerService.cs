using BtlThueXe.Core.DTOs.Customers;
using BtlThueXe.Core.Interfaces;
using BtlThueXe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BtlThueXe.Infrastructure.Services;

public class CustomerService : ICustomerService
{
    private readonly ApplicationDbContext _context;

    public CustomerService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CustomerProfileResponseDto?> GetProfileByUserIdAsync(int userId)
    {
        var kh = await _context.KhachHangs
            .Include(k => k.IdNguoiDungNavigation)
                .ThenInclude(u => u.IdVaiTros)
            .FirstOrDefaultAsync(k => k.IdNguoiDung == userId);

        if (kh == null) return null;

        return new CustomerProfileResponseDto
        {
            IdKhachHang = kh.Id,
            IdNguoiDung = kh.IdNguoiDung,
            HoTen = kh.IdNguoiDungNavigation.HoTen,
            Email = kh.IdNguoiDungNavigation.Email,
            SoDienThoai = kh.IdNguoiDungNavigation.SoDienThoai,
            SoCccd = kh.SoCccd,
            CccdDaXacMinh = kh.CccdDaXacMinh,
            DiaChi = kh.DiaChi,
            NgaySinh = kh.NgaySinh,
            Roles = kh.IdNguoiDungNavigation.IdVaiTros.Select(r => r.Ten).ToList()
        };
    }

    public async Task<List<CustomerProfileResponseDto>> GetAllCustomersAsync()
    {
        return await _context.KhachHangs
            .Include(k => k.IdNguoiDungNavigation)
                .ThenInclude(u => u.IdVaiTros)
            .Select(kh => new CustomerProfileResponseDto
            {
                IdKhachHang = kh.Id,
                IdNguoiDung = kh.IdNguoiDung,
                HoTen = kh.IdNguoiDungNavigation.HoTen,
                Email = kh.IdNguoiDungNavigation.Email,
                SoDienThoai = kh.IdNguoiDungNavigation.SoDienThoai,
                SoCccd = kh.SoCccd,
                CccdDaXacMinh = kh.CccdDaXacMinh,
                DiaChi = kh.DiaChi,
                NgaySinh = kh.NgaySinh,
                Roles = kh.IdNguoiDungNavigation.IdVaiTros.Select(r => r.Ten).ToList()
            })
            .ToListAsync();
    }

    public async Task<bool> VerifyCccdAsync(int customerId, bool daXacMinh)
    {
        var kh = await _context.KhachHangs.FindAsync(customerId);
        if (kh == null) return false;

        kh.CccdDaXacMinh = daXacMinh;
        kh.UpdatedAt = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);

        await _context.SaveChangesAsync();
        return true;
    }
}