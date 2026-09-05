using Microsoft.EntityFrameworkCore;
using ThueXe.Api.Models;

namespace ThueXe.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<HangXe> HangXes => Set<HangXe>();
    public DbSet<LoaiXe> LoaiXes => Set<LoaiXe>();
    public DbSet<Xe> Xes => Set<Xe>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Xe>()
            .HasOne(x => x.HangXe)
            .WithMany()
            .HasForeignKey(x => x.IdHangXe)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Xe>()
            .HasOne(x => x.LoaiXe)
            .WithMany()
            .HasForeignKey(x => x.IdLoaiXe)
            .OnDelete(DeleteBehavior.Restrict);
    }
}