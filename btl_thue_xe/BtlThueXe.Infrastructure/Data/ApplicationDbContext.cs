using System;
using System.Collections.Generic;
using BtlThueXe.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BtlThueXe.Infrastructure.Data;

public partial class ApplicationDbContext : DbContext
{
    public ApplicationDbContext()
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    // Để trống vì đã cấu hình chuỗi kết nối từ appsettings.json trong Program.cs
}
    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<BanGiaoXe> BanGiaoXes { get; set; }

    public virtual DbSet<DanhGium> DanhGia { get; set; }

    public virtual DbSet<HangXe> HangXes { get; set; }

    public virtual DbSet<HopDong> HopDongs { get; set; }

    public virtual DbSet<KhachHang> KhachHangs { get; set; }

    public virtual DbSet<LichSuTrangThaiHopDong> LichSuTrangThaiHopDongs { get; set; }

    public virtual DbSet<LichSuTrangThaiXe> LichSuTrangThaiXes { get; set; }

    public virtual DbSet<LoaiXe> LoaiXes { get; set; }

    public virtual DbSet<NguoiDung> NguoiDungs { get; set; }

    public virtual DbSet<Quyen> Quyens { get; set; }

    public virtual DbSet<ThanhToan> ThanhToans { get; set; }

    public virtual DbSet<TraXe> TraXes { get; set; }

    public virtual DbSet<VaiTro> VaiTros { get; set; }

    public virtual DbSet<Xe> Xes { get; set; }

    public virtual DbSet<YeuCauGiaHan> YeuCauGiaHans { get; set; }

    public virtual DbSet<YeuCauHuyHopDong> YeuCauHuyHopDongs { get; set; }

    public virtual DbSet<YeuCauThue> YeuCauThues { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("audit_log_pkey");

            entity.ToTable("audit_log");

            entity.HasIndex(e => e.IdNguoiDung, "idx_audit_nguoi_dung");

            entity.HasIndex(e => e.ThoiGian, "idx_audit_thoi_gian");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.DuLieuCu).HasColumnName("du_lieu_cu");
            entity.Property(e => e.DuLieuMoi).HasColumnName("du_lieu_moi");
            entity.Property(e => e.HanhDong)
                .HasMaxLength(50)
                .HasColumnName("hanh_dong");
            entity.Property(e => e.IdDoiTuong).HasColumnName("id_doi_tuong");
            entity.Property(e => e.IdNguoiDung).HasColumnName("id_nguoi_dung");
            entity.Property(e => e.IpAddress)
                .HasMaxLength(45)
                .HasColumnName("ip_address");
            entity.Property(e => e.LoaiDoiTuong)
                .HasMaxLength(100)
                .HasColumnName("loai_doi_tuong");
            entity.Property(e => e.MoTa)
                .HasMaxLength(1000)
                .HasColumnName("mo_ta");
            entity.Property(e => e.ThoiGian)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("thoi_gian");

            entity.HasOne(d => d.IdNguoiDungNavigation).WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.IdNguoiDung)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_audit_nguoi_dung");
        });

        modelBuilder.Entity<BanGiaoXe>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("ban_giao_xe_pkey");

            entity.ToTable("ban_giao_xe");

            entity.HasIndex(e => e.IdHopDong, "ban_giao_xe_id_hop_dong_key").IsUnique();

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.GhiChu)
                .HasMaxLength(1000)
                .HasColumnName("ghi_chu");
            entity.Property(e => e.IdHopDong).HasColumnName("id_hop_dong");
            entity.Property(e => e.IdNhanVien).HasColumnName("id_nhan_vien");
            entity.Property(e => e.IdXe).HasColumnName("id_xe");
            entity.Property(e => e.MucNhienLieu)
                .HasPrecision(5, 2)
                .HasColumnName("muc_nhien_lieu");
            entity.Property(e => e.SoKm).HasColumnName("so_km");
            entity.Property(e => e.ThoiGianGiao)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("thoi_gian_giao");
            entity.Property(e => e.TinhTrangXe)
                .HasMaxLength(1000)
                .HasColumnName("tinh_trang_xe");

            entity.HasOne(d => d.IdHopDongNavigation).WithOne(p => p.BanGiaoXe)
                .HasForeignKey<BanGiaoXe>(d => d.IdHopDong)
                .HasConstraintName("fk_bgx_hd");

            entity.HasOne(d => d.IdNhanVienNavigation).WithMany(p => p.BanGiaoXes)
                .HasForeignKey(d => d.IdNhanVien)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_bgx_nhan_vien");

            entity.HasOne(d => d.IdXeNavigation).WithMany(p => p.BanGiaoXes)
                .HasForeignKey(d => d.IdXe)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_bgx_xe");
        });

        modelBuilder.Entity<DanhGium>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("danh_gia_pkey");

            entity.ToTable("danh_gia");

            entity.HasIndex(e => e.IdHopDong, "danh_gia_id_hop_dong_key").IsUnique();

            entity.HasIndex(e => e.IdXe, "idx_danh_gia_xe");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.DiemDanhGia).HasColumnName("diem_danh_gia");
            entity.Property(e => e.IdHopDong).HasColumnName("id_hop_dong");
            entity.Property(e => e.IdKhachHang).HasColumnName("id_khach_hang");
            entity.Property(e => e.IdXe).HasColumnName("id_xe");
            entity.Property(e => e.NhanXet)
                .HasMaxLength(2000)
                .HasColumnName("nhan_xet");
            entity.Property(e => e.ThoiGianTao)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("thoi_gian_tao");

            entity.HasOne(d => d.IdHopDongNavigation).WithOne(p => p.DanhGium)
                .HasForeignKey<DanhGium>(d => d.IdHopDong)
                .HasConstraintName("fk_dg_hd");

            entity.HasOne(d => d.IdKhachHangNavigation).WithMany(p => p.DanhGia)
                .HasForeignKey(d => d.IdKhachHang)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_dg_khach_hang");

            entity.HasOne(d => d.IdXeNavigation).WithMany(p => p.DanhGia)
                .HasForeignKey(d => d.IdXe)
                .HasConstraintName("fk_dg_xe");
        });

        modelBuilder.Entity<HangXe>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("hang_xe_pkey");

            entity.ToTable("hang_xe");

            entity.HasIndex(e => e.Ten, "hang_xe_ten_key").IsUnique();

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.MoTa)
                .HasMaxLength(500)
                .HasColumnName("mo_ta");
            entity.Property(e => e.QuocGia)
                .HasMaxLength(100)
                .HasColumnName("quoc_gia");
            entity.Property(e => e.Ten)
                .HasMaxLength(100)
                .HasColumnName("ten");
        });

        modelBuilder.Entity<HopDong>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("hop_dong_pkey");

            entity.ToTable("hop_dong");

            entity.HasIndex(e => e.IdYeuCauThue, "hop_dong_id_yeu_cau_thue_key").IsUnique();

            entity.HasIndex(e => e.SoHopDong, "hop_dong_so_hop_dong_key").IsUnique();

            entity.HasIndex(e => new { e.ThoiGianNhanDuKien, e.ThoiGianTraDuKien }, "idx_hd_thoi_gian");

            entity.HasIndex(e => e.TrangThai, "idx_hd_trang_thai");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.DieuKhoan).HasColumnName("dieu_khoan");
            entity.Property(e => e.DonGiaNgay)
                .HasPrecision(18, 2)
                .HasColumnName("don_gia_ngay");
            entity.Property(e => e.IdNhanVienLap).HasColumnName("id_nhan_vien_lap");
            entity.Property(e => e.IdYeuCauThue).HasColumnName("id_yeu_cau_thue");
            entity.Property(e => e.SoHopDong)
                .HasMaxLength(50)
                .HasColumnName("so_hop_dong");
            entity.Property(e => e.SoNgayThue).HasColumnName("so_ngay_thue");
            entity.Property(e => e.ThoiGianCapNhat)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("thoi_gian_cap_nhat");
            entity.Property(e => e.ThoiGianNhanDuKien)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("thoi_gian_nhan_du_kien");
            entity.Property(e => e.ThoiGianNhanThucTe)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("thoi_gian_nhan_thuc_te");
            entity.Property(e => e.ThoiGianTao)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("thoi_gian_tao");
            entity.Property(e => e.ThoiGianTraDuKien)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("thoi_gian_tra_du_kien");
            entity.Property(e => e.ThoiGianTraThucTe)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("thoi_gian_tra_thuc_te");
            entity.Property(e => e.TienCoc)
                .HasPrecision(18, 2)
                .HasColumnName("tien_coc");
            entity.Property(e => e.TienThue)
                .HasPrecision(18, 2)
                .HasColumnName("tien_thue");
            entity.Property(e => e.TongTien)
                .HasPrecision(18, 2)
                .HasColumnName("tong_tien");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(40)
                .HasDefaultValueSql("'DRAFT'::character varying")
                .HasColumnName("trang_thai");

            entity.HasOne(d => d.IdNhanVienLapNavigation).WithMany(p => p.HopDongs)
                .HasForeignKey(d => d.IdNhanVienLap)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_hd_nhan_vien");

            entity.HasOne(d => d.IdYeuCauThueNavigation).WithOne(p => p.HopDong)
                .HasForeignKey<HopDong>(d => d.IdYeuCauThue)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_hd_yct");
        });

        modelBuilder.Entity<KhachHang>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("khach_hang_pkey");

            entity.ToTable("khach_hang");

            entity.HasIndex(e => e.IdNguoiDung, "khach_hang_id_nguoi_dung_key").IsUnique();

            entity.HasIndex(e => e.SoCccd, "khach_hang_so_cccd_key").IsUnique();

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.CccdDaXacMinh).HasColumnName("cccd_da_xac_minh");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.DiaChi)
                .HasMaxLength(500)
                .HasColumnName("dia_chi");
            entity.Property(e => e.IdNguoiDung).HasColumnName("id_nguoi_dung");
            entity.Property(e => e.NgaySinh).HasColumnName("ngay_sinh");
            entity.Property(e => e.SoCccd)
                .HasMaxLength(20)
                .HasColumnName("so_cccd");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.IdNguoiDungNavigation).WithOne(p => p.KhachHang)
                .HasForeignKey<KhachHang>(d => d.IdNguoiDung)
                .HasConstraintName("fk_khach_hang_nguoi_dung");
        });

        modelBuilder.Entity<LichSuTrangThaiHopDong>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("lich_su_trang_thai_hop_dong_pkey");

            entity.ToTable("lich_su_trang_thai_hop_dong");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.IdHopDong).HasColumnName("id_hop_dong");
            entity.Property(e => e.IdNguoiThayDoi).HasColumnName("id_nguoi_thay_doi");
            entity.Property(e => e.LyDo)
                .HasMaxLength(500)
                .HasColumnName("ly_do");
            entity.Property(e => e.ThoiGianThayDoi)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("thoi_gian_thay_doi");
            entity.Property(e => e.TrangThaiCu)
                .HasMaxLength(40)
                .HasColumnName("trang_thai_cu");
            entity.Property(e => e.TrangThaiMoi)
                .HasMaxLength(40)
                .HasColumnName("trang_thai_moi");

            entity.HasOne(d => d.IdHopDongNavigation).WithMany(p => p.LichSuTrangThaiHopDongs)
                .HasForeignKey(d => d.IdHopDong)
                .HasConstraintName("fk_lsht_hd");

            entity.HasOne(d => d.IdNguoiThayDoiNavigation).WithMany(p => p.LichSuTrangThaiHopDongs)
                .HasForeignKey(d => d.IdNguoiThayDoi)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_lsht_nguoi_dung");
        });

        modelBuilder.Entity<LichSuTrangThaiXe>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("lich_su_trang_thai_xe_pkey");

            entity.ToTable("lich_su_trang_thai_xe");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.IdNguoiThayDoi).HasColumnName("id_nguoi_thay_doi");
            entity.Property(e => e.IdXe).HasColumnName("id_xe");
            entity.Property(e => e.LyDo)
                .HasMaxLength(500)
                .HasColumnName("ly_do");
            entity.Property(e => e.ThoiGianThayDoi)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("thoi_gian_thay_doi");
            entity.Property(e => e.TrangThaiCu)
                .HasMaxLength(30)
                .HasColumnName("trang_thai_cu");
            entity.Property(e => e.TrangThaiMoi)
                .HasMaxLength(30)
                .HasColumnName("trang_thai_moi");

            entity.HasOne(d => d.IdNguoiThayDoiNavigation).WithMany(p => p.LichSuTrangThaiXes)
                .HasForeignKey(d => d.IdNguoiThayDoi)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_lsstx_nguoi_dung");

            entity.HasOne(d => d.IdXeNavigation).WithMany(p => p.LichSuTrangThaiXes)
                .HasForeignKey(d => d.IdXe)
                .HasConstraintName("fk_lsstx_xe");
        });

        modelBuilder.Entity<LoaiXe>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("loai_xe_pkey");

            entity.ToTable("loai_xe");

            entity.HasIndex(e => e.Ten, "loai_xe_ten_key").IsUnique();

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.MoTa)
                .HasMaxLength(500)
                .HasColumnName("mo_ta");
            entity.Property(e => e.SoCho).HasColumnName("so_cho");
            entity.Property(e => e.Ten)
                .HasMaxLength(100)
                .HasColumnName("ten");
        });

        modelBuilder.Entity<NguoiDung>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("nguoi_dung_pkey");

            entity.ToTable("nguoi_dung");

            entity.HasIndex(e => e.Email, "nguoi_dung_email_key").IsUnique();

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.DangHoatDong)
                .HasDefaultValue(true)
                .HasColumnName("dang_hoat_dong");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.HoTen)
                .HasMaxLength(100)
                .HasColumnName("ho_ten");
            entity.Property(e => e.MatKhau)
                .HasMaxLength(255)
                .HasColumnName("mat_khau");
            entity.Property(e => e.SoDienThoai)
                .HasMaxLength(20)
                .HasColumnName("so_dien_thoai");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("updated_at");

            entity.HasMany(d => d.IdVaiTros).WithMany(p => p.IdNguoiDungs)
                .UsingEntity<Dictionary<string, object>>(
                    "NguoiDungVaiTro",
                    r => r.HasOne<VaiTro>().WithMany()
                        .HasForeignKey("IdVaiTro")
                        .HasConstraintName("fk_ndvt_vai_tro"),
                    l => l.HasOne<NguoiDung>().WithMany()
                        .HasForeignKey("IdNguoiDung")
                        .HasConstraintName("fk_ndvt_nguoi_dung"),
                    j =>
                    {
                        j.HasKey("IdNguoiDung", "IdVaiTro").HasName("nguoi_dung_vai_tro_pkey");
                        j.ToTable("nguoi_dung_vai_tro");
                        j.IndexerProperty<int>("IdNguoiDung").HasColumnName("id_nguoi_dung");
                        j.IndexerProperty<int>("IdVaiTro").HasColumnName("id_vai_tro");
                    });
        });

        modelBuilder.Entity<Quyen>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("quyen_pkey");

            entity.ToTable("quyen");

            entity.HasIndex(e => e.MaQuyen, "quyen_ma_quyen_key").IsUnique();

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.MaQuyen)
                .HasMaxLength(100)
                .HasColumnName("ma_quyen");
            entity.Property(e => e.MoTa)
                .HasMaxLength(255)
                .HasColumnName("mo_ta");
        });

        modelBuilder.Entity<ThanhToan>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("thanh_toan_pkey");

            entity.ToTable("thanh_toan");

            entity.HasIndex(e => e.IdHopDong, "idx_tt_hop_dong");

            entity.HasIndex(e => e.TrangThai, "idx_tt_trang_thai");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.GhiChu)
                .HasMaxLength(500)
                .HasColumnName("ghi_chu");
            entity.Property(e => e.IdHopDong).HasColumnName("id_hop_dong");
            entity.Property(e => e.LoaiThanhToan)
                .HasMaxLength(30)
                .HasColumnName("loai_thanh_toan");
            entity.Property(e => e.MaGiaoDich)
                .HasMaxLength(100)
                .HasColumnName("ma_giao_dich");
            entity.Property(e => e.PhuongThuc)
                .HasMaxLength(30)
                .HasColumnName("phuong_thuc");
            entity.Property(e => e.SoTien)
                .HasPrecision(18, 2)
                .HasColumnName("so_tien");
            entity.Property(e => e.ThoiGianThanhToan)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("thoi_gian_thanh_toan");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(30)
                .HasDefaultValueSql("'PENDING'::character varying")
                .HasColumnName("trang_thai");

            entity.HasOne(d => d.IdHopDongNavigation).WithMany(p => p.ThanhToans)
                .HasForeignKey(d => d.IdHopDong)
                .HasConstraintName("fk_tt_hd");
        });

        modelBuilder.Entity<TraXe>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("tra_xe_pkey");

            entity.ToTable("tra_xe");

            entity.HasIndex(e => e.IdHopDong, "tra_xe_id_hop_dong_key").IsUnique();

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.GhiChu)
                .HasMaxLength(1000)
                .HasColumnName("ghi_chu");
            entity.Property(e => e.IdHopDong).HasColumnName("id_hop_dong");
            entity.Property(e => e.IdNhanVien).HasColumnName("id_nhan_vien");
            entity.Property(e => e.IdXe).HasColumnName("id_xe");
            entity.Property(e => e.MucNhienLieu)
                .HasPrecision(5, 2)
                .HasColumnName("muc_nhien_lieu");
            entity.Property(e => e.PhiPhatSinh)
                .HasPrecision(18, 2)
                .HasColumnName("phi_phat_sinh");
            entity.Property(e => e.PhiTraMuon)
                .HasPrecision(18, 2)
                .HasColumnName("phi_tra_muon");
            entity.Property(e => e.SoKm).HasColumnName("so_km");
            entity.Property(e => e.ThoiGianTao)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("thoi_gian_tao");
            entity.Property(e => e.ThoiGianTraDuKien)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("thoi_gian_tra_du_kien");
            entity.Property(e => e.ThoiGianTraThucTe)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("thoi_gian_tra_thuc_te");
            entity.Property(e => e.TinhTrangXe)
                .HasMaxLength(1000)
                .HasColumnName("tinh_trang_xe");
            entity.Property(e => e.TongPhiPhatSinh)
                .HasPrecision(18, 2)
                .HasColumnName("tong_phi_phat_sinh");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(30)
                .HasDefaultValueSql("'COMPLETED'::character varying")
                .HasColumnName("trang_thai");

            entity.HasOne(d => d.IdHopDongNavigation).WithOne(p => p.TraXe)
                .HasForeignKey<TraXe>(d => d.IdHopDong)
                .HasConstraintName("fk_tx_hd");

            entity.HasOne(d => d.IdNhanVienNavigation).WithMany(p => p.TraXes)
                .HasForeignKey(d => d.IdNhanVien)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_tx_nhan_vien");

            entity.HasOne(d => d.IdXeNavigation).WithMany(p => p.TraXes)
                .HasForeignKey(d => d.IdXe)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_tx_xe");
        });

        modelBuilder.Entity<VaiTro>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("vai_tro_pkey");

            entity.ToTable("vai_tro");

            entity.HasIndex(e => e.Ten, "vai_tro_ten_key").IsUnique();

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.MoTa)
                .HasMaxLength(255)
                .HasColumnName("mo_ta");
            entity.Property(e => e.Ten)
                .HasMaxLength(50)
                .HasColumnName("ten");

            entity.HasMany(d => d.IdQuyens).WithMany(p => p.IdVaiTros)
                .UsingEntity<Dictionary<string, object>>(
                    "VaiTroQuyen",
                    r => r.HasOne<Quyen>().WithMany()
                        .HasForeignKey("IdQuyen")
                        .HasConstraintName("fk_vtq_quyen"),
                    l => l.HasOne<VaiTro>().WithMany()
                        .HasForeignKey("IdVaiTro")
                        .HasConstraintName("fk_vtq_vai_tro"),
                    j =>
                    {
                        j.HasKey("IdVaiTro", "IdQuyen").HasName("vai_tro_quyen_pkey");
                        j.ToTable("vai_tro_quyen");
                        j.IndexerProperty<int>("IdVaiTro").HasColumnName("id_vai_tro");
                        j.IndexerProperty<int>("IdQuyen").HasColumnName("id_quyen");
                    });
        });

        modelBuilder.Entity<Xe>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("xe_pkey");

            entity.ToTable("xe");

            entity.HasIndex(e => e.IdHangXe, "idx_xe_hang");

            entity.HasIndex(e => e.IdLoaiXe, "idx_xe_loai");

            entity.HasIndex(e => e.TrangThai, "idx_xe_trang_thai");

            entity.HasIndex(e => e.BienSoXe, "xe_bien_so_xe_key").IsUnique();

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.BienSoXe)
                .HasMaxLength(20)
                .HasColumnName("bien_so_xe");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.DonGiaNgay)
                .HasPrecision(18, 2)
                .HasColumnName("don_gia_ngay");
            entity.Property(e => e.IdHangXe).HasColumnName("id_hang_xe");
            entity.Property(e => e.IdLoaiXe).HasColumnName("id_loai_xe");
            entity.Property(e => e.MauXe)
                .HasMaxLength(50)
                .HasColumnName("mau_xe");
            entity.Property(e => e.MoTa)
                .HasMaxLength(1000)
                .HasColumnName("mo_ta");
            entity.Property(e => e.NamSanXuat).HasColumnName("nam_san_xuat");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(30)
                .HasDefaultValueSql("'AVAILABLE'::character varying")
                .HasColumnName("trang_thai");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.IdHangXeNavigation).WithMany(p => p.Xes)
                .HasForeignKey(d => d.IdHangXe)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_xe_hang");

            entity.HasOne(d => d.IdLoaiXeNavigation).WithMany(p => p.Xes)
                .HasForeignKey(d => d.IdLoaiXe)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_xe_loai");
        });

        modelBuilder.Entity<YeuCauGiaHan>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("yeu_cau_gia_han_pkey");

            entity.ToTable("yeu_cau_gia_han");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.IdHopDong).HasColumnName("id_hop_dong");
            entity.Property(e => e.IdNguoiXuLy).HasColumnName("id_nguoi_xu_ly");
            entity.Property(e => e.IdNguoiYeuCau).HasColumnName("id_nguoi_yeu_cau");
            entity.Property(e => e.LyDo)
                .HasMaxLength(500)
                .HasColumnName("ly_do");
            entity.Property(e => e.SoNgayGiaHan).HasColumnName("so_ngay_gia_han");
            entity.Property(e => e.ThoiGianTao)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("thoi_gian_tao");
            entity.Property(e => e.ThoiGianTraCu)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("thoi_gian_tra_cu");
            entity.Property(e => e.ThoiGianTraMoi)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("thoi_gian_tra_moi");
            entity.Property(e => e.ThoiGianXuLy)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("thoi_gian_xu_ly");
            entity.Property(e => e.TienPhatSinh)
                .HasPrecision(18, 2)
                .HasColumnName("tien_phat_sinh");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .HasDefaultValueSql("'PENDING'::character varying")
                .HasColumnName("trang_thai");

            entity.HasOne(d => d.IdHopDongNavigation).WithMany(p => p.YeuCauGiaHans)
                .HasForeignKey(d => d.IdHopDong)
                .HasConstraintName("fk_ycgh_hd");

            entity.HasOne(d => d.IdNguoiXuLyNavigation).WithMany(p => p.YeuCauGiaHanIdNguoiXuLyNavigations)
                .HasForeignKey(d => d.IdNguoiXuLy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_ycgh_nguoi_xu_ly");

            entity.HasOne(d => d.IdNguoiYeuCauNavigation).WithMany(p => p.YeuCauGiaHanIdNguoiYeuCauNavigations)
                .HasForeignKey(d => d.IdNguoiYeuCau)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_ycgh_nguoi_yeu_cau");
        });

        modelBuilder.Entity<YeuCauHuyHopDong>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("yeu_cau_huy_hop_dong_pkey");

            entity.ToTable("yeu_cau_huy_hop_dong");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.IdHopDong).HasColumnName("id_hop_dong");
            entity.Property(e => e.IdNguoiXuLy).HasColumnName("id_nguoi_xu_ly");
            entity.Property(e => e.IdNguoiYeuCau).HasColumnName("id_nguoi_yeu_cau");
            entity.Property(e => e.LyDo)
                .HasMaxLength(1000)
                .HasColumnName("ly_do");
            entity.Property(e => e.SoTienHoanDuKien)
                .HasPrecision(18, 2)
                .HasColumnName("so_tien_hoan_du_kien");
            entity.Property(e => e.SoTienPhat)
                .HasPrecision(18, 2)
                .HasColumnName("so_tien_phat");
            entity.Property(e => e.ThoiGianTao)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("thoi_gian_tao");
            entity.Property(e => e.ThoiGianXuLy)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("thoi_gian_xu_ly");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .HasDefaultValueSql("'PENDING'::character varying")
                .HasColumnName("trang_thai");

            entity.HasOne(d => d.IdHopDongNavigation).WithMany(p => p.YeuCauHuyHopDongs)
                .HasForeignKey(d => d.IdHopDong)
                .HasConstraintName("fk_ych_hd");

            entity.HasOne(d => d.IdNguoiXuLyNavigation).WithMany(p => p.YeuCauHuyHopDongIdNguoiXuLyNavigations)
                .HasForeignKey(d => d.IdNguoiXuLy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_ych_nguoi_xu_ly");

            entity.HasOne(d => d.IdNguoiYeuCauNavigation).WithMany(p => p.YeuCauHuyHopDongIdNguoiYeuCauNavigations)
                .HasForeignKey(d => d.IdNguoiYeuCau)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_ych_nguoi_yeu_cau");
        });

        modelBuilder.Entity<YeuCauThue>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("yeu_cau_thue_pkey");

            entity.ToTable("yeu_cau_thue");

            entity.HasIndex(e => e.IdKhachHang, "idx_yct_khach_hang");

            entity.HasIndex(e => e.TrangThai, "idx_yct_trang_thai");

            entity.HasIndex(e => e.IdXe, "idx_yct_xe");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.IdKhachHang).HasColumnName("id_khach_hang");
            entity.Property(e => e.IdNhanVienXuLy).HasColumnName("id_nhan_vien_xu_ly");
            entity.Property(e => e.IdXe).HasColumnName("id_xe");
            entity.Property(e => e.LyDoHuy)
                .HasMaxLength(500)
                .HasColumnName("ly_do_huy");
            entity.Property(e => e.LyDoTuChoi)
                .HasMaxLength(500)
                .HasColumnName("ly_do_tu_choi");
            entity.Property(e => e.Nguon)
                .HasMaxLength(20)
                .HasColumnName("nguon");
            entity.Property(e => e.ThoiGianCapNhat)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("thoi_gian_cap_nhat");
            entity.Property(e => e.ThoiGianNhan)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("thoi_gian_nhan");
            entity.Property(e => e.ThoiGianTao)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("thoi_gian_tao");
            entity.Property(e => e.ThoiGianTraDuKien)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("thoi_gian_tra_du_kien");
            entity.Property(e => e.TienCoc)
                .HasPrecision(18, 2)
                .HasColumnName("tien_coc");
            entity.Property(e => e.TienUocTinh)
                .HasPrecision(18, 2)
                .HasColumnName("tien_uoc_tinh");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(30)
                .HasDefaultValueSql("'PENDING'::character varying")
                .HasColumnName("trang_thai");

            entity.HasOne(d => d.IdKhachHangNavigation).WithMany(p => p.YeuCauThues)
                .HasForeignKey(d => d.IdKhachHang)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_yct_khach_hang");

            entity.HasOne(d => d.IdNhanVienXuLyNavigation).WithMany(p => p.YeuCauThues)
                .HasForeignKey(d => d.IdNhanVienXuLy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_yct_nhan_vien");

            entity.HasOne(d => d.IdXeNavigation).WithMany(p => p.YeuCauThues)
                .HasForeignKey(d => d.IdXe)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_yct_xe");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
