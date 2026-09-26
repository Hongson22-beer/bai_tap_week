using BtlThueXe.Core.DTOs.Customers;
using BtlThueXe.Core.Interfaces;
using BtlThueXe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BtlThueXe.Infrastructure.Services;

public class CustomerService : ICustomerService
{
    private readonly ApplicationDbContext _context;
    private readonly string _uploadRoot = Path.Combine(AppContext.BaseDirectory, "private_uploads", "identity");
    public CustomerService(ApplicationDbContext context) { _context = context; Directory.CreateDirectory(_uploadRoot); }

    private static CustomerProfileResponseDto Map(KhachHang kh) => new()
    {
        IdKhachHang=kh.Id, IdNguoiDung=kh.IdNguoiDung, HoTen=kh.IdNguoiDungNavigation.HoTen,
        Email=kh.IdNguoiDungNavigation.Email, SoDienThoai=kh.IdNguoiDungNavigation.SoDienThoai??"",
        SoCccd=kh.SoCccd, CccdDaXacMinh=kh.CccdDaXacMinh, AnhCccdMatTruoc=kh.AnhCccdMatTruoc,
        AnhCccdMatSau=kh.AnhCccdMatSau, SoGplx=kh.SoGplx, AnhGplx=kh.AnhGplx, GplxDaXacMinh=kh.GplxDaXacMinh,
        NganHang=kh.NganHang, SoTaiKhoan=kh.SoTaiKhoan, ChuTaiKhoan=kh.ChuTaiKhoan,
        DiaChi=kh.DiaChi, NgaySinh=kh.NgaySinh, Roles=kh.IdNguoiDungNavigation.IdVaiTros.Select(r=>r.Ten).ToList()
    };

    public async Task<CustomerProfileResponseDto?> GetProfileByUserIdAsync(int userId)
    {
        var kh=await _context.KhachHangs.Include(k=>k.IdNguoiDungNavigation).ThenInclude(u=>u.IdVaiTros).FirstOrDefaultAsync(k=>k.IdNguoiDung==userId);
        return kh==null?null:Map(kh);
    }
    public async Task<List<CustomerProfileResponseDto>> GetAllCustomersAsync()
    {
        var rows=await _context.KhachHangs.Include(k=>k.IdNguoiDungNavigation).ThenInclude(u=>u.IdVaiTros).ToListAsync();
        return rows.Select(Map).ToList();
    }
    public async Task<CustomerProfileResponseDto> UpdateProfileAsync(int userId, UpdateCustomerProfileRequestDto dto)
    {
        var kh=await _context.KhachHangs.Include(k=>k.IdNguoiDungNavigation).ThenInclude(u=>u.IdVaiTros).FirstOrDefaultAsync(k=>k.IdNguoiDung==userId) ?? throw new KeyNotFoundException("Không tìm thấy hồ sơ khách hàng.");
        var newCccd=(dto.SoCccd??"").Trim(); var newGplx=(dto.SoGplx??"").Trim();
        if(string.IsNullOrWhiteSpace(newCccd)) throw new ArgumentException("Số CCCD không được để trống.");
        if(await _context.KhachHangs.AnyAsync(x=>x.Id!=kh.Id && x.SoCccd==newCccd)) throw new ArgumentException("Số CCCD đã tồn tại.");
        if(kh.SoCccd!=newCccd) kh.CccdDaXacMinh=false;
        if(!string.Equals(kh.SoGplx,newGplx,StringComparison.OrdinalIgnoreCase)) kh.GplxDaXacMinh=false;
        kh.SoCccd=newCccd; kh.SoGplx=string.IsNullOrWhiteSpace(newGplx)?null:newGplx;
        kh.DiaChi=dto.DiaChi?.Trim(); kh.NgaySinh=dto.NgaySinh; kh.NganHang=dto.NganHang?.Trim(); kh.SoTaiKhoan=dto.SoTaiKhoan?.Trim(); kh.ChuTaiKhoan=dto.ChuTaiKhoan?.Trim();
        kh.IdNguoiDungNavigation.SoDienThoai=dto.SoDienThoai?.Trim(); kh.UpdatedAt=DateTime.UtcNow; kh.IdNguoiDungNavigation.UpdatedAt=DateTime.UtcNow;
        await _context.SaveChangesAsync(); return Map(kh);
    }
    public async Task<bool> VerifyCccdAsync(int customerId,bool daXacMinh) => await VerifyPart(customerId,daXacMinh,null);
    public async Task<bool> VerifyDocumentsAsync(int customerId,bool cccd,bool gplx) => await VerifyPart(customerId,cccd,gplx);
    private async Task<bool> VerifyPart(int id,bool cccd,bool? gplx)
    { var kh=await _context.KhachHangs.FindAsync(id); if(kh==null)return false; kh.CccdDaXacMinh=cccd; if(gplx.HasValue)kh.GplxDaXacMinh=gplx.Value; kh.UpdatedAt=DateTime.UtcNow; await _context.SaveChangesAsync(); return true; }

    public async Task<string> SaveDocumentAsync(int userId,string documentType,Stream stream,string extension)
    {
        var kh=await _context.KhachHangs.FirstOrDefaultAsync(x=>x.IdNguoiDung==userId) ?? throw new KeyNotFoundException("Không tìm thấy hồ sơ khách hàng.");
        var allowed=new[]{"cccd-front","cccd-back","gplx"}; if(!allowed.Contains(documentType)) throw new ArgumentException("Loại giấy tờ không hợp lệ.");
        var dir=Path.Combine(_uploadRoot,kh.Id.ToString()); Directory.CreateDirectory(dir); var file=$"{documentType}{extension.ToLowerInvariant()}"; var path=Path.Combine(dir,file);
        await using(var fs=File.Create(path)){await stream.CopyToAsync(fs);} var token=$"{kh.Id}/{file}";
        if(documentType=="cccd-front"){kh.AnhCccdMatTruoc=token;kh.CccdDaXacMinh=false;} else if(documentType=="cccd-back"){kh.AnhCccdMatSau=token;kh.CccdDaXacMinh=false;} else {kh.AnhGplx=token;kh.GplxDaXacMinh=false;}
        kh.UpdatedAt=DateTime.UtcNow; await _context.SaveChangesAsync(); return token;
    }
    public async Task<(byte[] Bytes,string ContentType)?> GetDocumentAsync(int userId,string documentType,bool staffCanRead)
    {
        KhachHang? kh; if(staffCanRead && int.TryParse(userId.ToString(),out var customerId)) kh=await _context.KhachHangs.FindAsync(customerId); else kh=await _context.KhachHangs.FirstOrDefaultAsync(x=>x.IdNguoiDung==userId);
        if(kh==null)return null; string? token=documentType switch{"cccd-front"=>kh.AnhCccdMatTruoc,"cccd-back"=>kh.AnhCccdMatSau,"gplx"=>kh.AnhGplx,_=>null}; if(string.IsNullOrWhiteSpace(token))return null;
        var path=Path.Combine(_uploadRoot,token.Replace('/',Path.DirectorySeparatorChar)); if(!File.Exists(path))return null; var ext=Path.GetExtension(path).ToLowerInvariant(); return (await File.ReadAllBytesAsync(path),ext==".png"?"image/png":"image/jpeg");
    }
}
