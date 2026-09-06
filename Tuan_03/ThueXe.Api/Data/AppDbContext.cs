using Microsoft.EntityFrameworkCore;
using ThueXe.Api.Models;

namespace ThueXe.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<HangXe> HangXes => Set<HangXe>();
    public DbSet<LoaiXe> LoaiXes => Set<LoaiXe>();
    public DbSet<Xe> Xes => Set<Xe>();

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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

        modelBuilder.Entity<UserRole>()
            .HasKey(ur => new { ur.UserId, ur.RoleId });

        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId);

        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId);
    }
}