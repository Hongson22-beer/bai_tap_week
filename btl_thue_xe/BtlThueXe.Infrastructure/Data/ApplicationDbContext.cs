using BtlThueXe.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace BtlThueXe.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<ThanhToan> ThanhToans => Set<ThanhToan>();

    public DbSet<BanGiaoXe> BanGiaoXes => Set<BanGiaoXe>();

    public DbSet<TraXe> TraXes => Set<TraXe>();

    public DbSet<YeuCauGiaHan> YeuCauGiaHans => Set<YeuCauGiaHan>();

    public DbSet<YeuCauHuyHopDong> YeuCauHuyHopDongs
        => Set<YeuCauHuyHopDong>();

    public DbSet<DanhGia> DanhGias => Set<DanhGia>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
}