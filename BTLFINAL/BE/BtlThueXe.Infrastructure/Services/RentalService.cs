using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BtlThueXe.Core.DTOs.Common;
using BtlThueXe.Core.DTOs.AuditLogs;
using BtlThueXe.Core.Interfaces;
using System.Data;
using BtlThueXe.Core.DTOs.Rentals;
using BtlThueXe.Core.Services;
using BtlThueXe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BtlThueXe.Infrastructure.Services
{
    public class RentalService : IRentalService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditLogService _auditLogService;

        public RentalService(
            ApplicationDbContext context,
            IAuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }

        #region Helper Methods - Ownership & Role Validations

        private static void ValidateStaffRole(string? role)
        {
            if (!string.Equals(
                    role,
                    "NHAN_VIEN",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException(
                    "Thao tác này chỉ dành riêng cho Nhân viên (NHAN_VIEN).");
            }
        }

        private async Task<KhachHang> GetCustomerByUserIdAsync(int userId)
        {
            var customer = await _context.KhachHangs
                .FirstOrDefaultAsync(k => k.IdNguoiDung == userId);

            if (customer == null)
            {
                throw new KeyNotFoundException(
                    "Không tìm thấy thông tin khách hàng tương ứng với tài khoản này.");
            }

            return customer;
        }

        private static void ValidateRentalDates(
            DateTime thoiGianNhan,
            DateTime thoiGianTraDuKien)
        {
            if (thoiGianTraDuKien <= thoiGianNhan)
            {
                throw new InvalidOperationException(
                    "Thời gian trả dự kiến phải lớn hơn thời gian nhận.");
            }
        }

        private async Task<Xe> GetAndValidateXeAsync(int idXe)
        {
            var xe = await _context.Xes.FindAsync(idXe);

            if (xe == null)
            {
                throw new KeyNotFoundException(
                    "Không tìm thấy thông tin xe.");
            }

            if (string.Equals(
                    xe.TrangThai,
                    "MAINTENANCE",
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    xe.TrangThai,
                    "INACTIVE",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Xe đang bảo trì hoặc ngưng hoạt động, không thể đặt.");
            }

            return xe;
        }

        private async Task CheckAndThrowIfOverlapAsync(
            int idXe,
            DateTime thoiGianNhan,
            DateTime thoiGianTraDuKien)
        {
            bool isOverlap = await CheckRentalOverlapAsync(
                idXe,
                thoiGianNhan,
                thoiGianTraDuKien);

            if (isOverlap)
            {
                throw new InvalidOperationException(
                    "Xe đã có lịch thuê trong khoảng thời gian này.");
            }
        }

        private static (decimal TienUocTinh, decimal TienCoc)
            CalculateRentalFee(
                decimal donGiaNgay,
                DateTime start,
                DateTime end)
        {
            var totalHours = (end - start).TotalHours;

            int rentalDays =
                (int)Math.Ceiling(totalHours / 24.0);

            if (rentalDays <= 0)
            {
                rentalDays = 1;
            }

            decimal tienUocTinh =
                rentalDays * donGiaNgay;

            // Chính sách đã chốt: tiền cọc = 30% tiền thuê dự kiến.
            // Tiền cọc là khoản thanh toán trước và sẽ được trừ khi quyết toán cuối.
            decimal tienCoc = Math.Round(tienUocTinh * 0.30m, 0, MidpointRounding.AwayFromZero);

            return (tienUocTinh, tienCoc);
        }

        #endregion


        #region Create Online Rental

        public async Task<RentalResponseDto> CreateOnlineRentalAsync(
            CreateRentalRequestDto request,
            int currentUserId)
        {
            ValidateRentalDates(
                request.ThoiGianNhan,
                request.ThoiGianTraDuKien);

            var hinhThucNhanXe = (request.HinhThucNhanXe ?? "TAI_CUA_HANG").Trim().ToUpperInvariant();
            if (hinhThucNhanXe != "TAI_CUA_HANG" && hinhThucNhanXe != "GIAO_TAN_NOI")
                throw new InvalidOperationException("Hình thức nhận xe không hợp lệ.");

            if (hinhThucNhanXe == "GIAO_TAN_NOI")
            {
                if (string.IsNullOrWhiteSpace(request.TenNguoiNhan) ||
                    string.IsNullOrWhiteSpace(request.SoDienThoaiNhan) ||
                    string.IsNullOrWhiteSpace(request.DiaChiGiaoXe))
                    throw new InvalidOperationException("Giao tận nơi cần người nhận, số điện thoại và địa chỉ giao xe.");
            }

            var khachHang =
                await GetCustomerByUserIdAsync(currentUserId);

            if (!khachHang.CccdDaXacMinh || !khachHang.GplxDaXacMinh ||
                string.IsNullOrWhiteSpace(khachHang.AnhCccdMatTruoc) ||
                string.IsNullOrWhiteSpace(khachHang.AnhCccdMatSau) ||
                string.IsNullOrWhiteSpace(khachHang.AnhGplx))
            {
                throw new InvalidOperationException(
                    "Hồ sơ thuê xe chưa được xác minh đầy đủ CCCD và GPLX, không thể tạo yêu cầu thuê ONLINE.");
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            var xe =
                await GetAndValidateXeAsync(request.IdXe);

            await CheckAndThrowIfOverlapAsync(
                xe.Id,
                request.ThoiGianNhan,
                request.ThoiGianTraDuKien);

            var (tienUocTinh, tienCoc) =
                CalculateRentalFee(
                    xe.DonGiaNgay,
                    request.ThoiGianNhan,
                    request.ThoiGianTraDuKien);

            var now = DateTime.Now;

            var rental = new YeuCauThue
            {
                IdKhachHang = khachHang.Id,
                IdXe = xe.Id,
                Nguon = "ONLINE",
                ThoiGianNhan = request.ThoiGianNhan,
                ThoiGianTraDuKien =
                    request.ThoiGianTraDuKien,
                TrangThai = "PENDING",
                TienUocTinh = tienUocTinh,
                TienCoc = tienCoc,
                HinhThucNhanXe = hinhThucNhanXe,
                TenNguoiNhan = hinhThucNhanXe == "GIAO_TAN_NOI" ? request.TenNguoiNhan?.Trim() : null,
                SoDienThoaiNhan = hinhThucNhanXe == "GIAO_TAN_NOI" ? request.SoDienThoaiNhan?.Trim() : null,
                DiaChiGiaoXe = hinhThucNhanXe == "GIAO_TAN_NOI" ? request.DiaChiGiaoXe?.Trim() : null,
                GhiChuGiaoXe = request.GhiChuGiaoXe?.Trim(),
                ThoiGianTao = now,
                ThoiGianCapNhat = now
            };

            _context.YeuCauThues.Add(rental);

            await _context.SaveChangesAsync();

            await WriteAuditAsync(
                currentUserId,
                rental,
                "RENTAL_CREATED_ONLINE");

            await transaction.CommitAsync();

            return MapToResponseDto(rental);
        }

        #endregion


        #region Create Offline Rental

        public async Task<RentalResponseDto> CreateOfflineRentalAsync(
            CreateOfflineRentalRequestDto request,
            int staffUserId,
            string? currentUserRole)
        {
            ValidateStaffRole(currentUserRole);

            ValidateRentalDates(
                request.ThoiGianNhan,
                request.ThoiGianTraDuKien);

            var khachHang =
                await _context.KhachHangs.FindAsync(
                    request.IdKhachHang);

            if (khachHang == null)
            {
                throw new KeyNotFoundException(
                    "Khách hàng không tồn tại.");
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            var xe =
                await GetAndValidateXeAsync(request.IdXe);

            await CheckAndThrowIfOverlapAsync(
                xe.Id,
                request.ThoiGianNhan,
                request.ThoiGianTraDuKien);

            var (tienUocTinh, tienCoc) =
                CalculateRentalFee(
                    xe.DonGiaNgay,
                    request.ThoiGianNhan,
                    request.ThoiGianTraDuKien);

            var now = DateTime.Now;

            var rental = new YeuCauThue
            {
                IdKhachHang = request.IdKhachHang,
                IdXe = request.IdXe,
                Nguon = "OFFLINE",
                ThoiGianNhan = request.ThoiGianNhan,
                ThoiGianTraDuKien =
                    request.ThoiGianTraDuKien,
                TrangThai = "PENDING",
                TienUocTinh = tienUocTinh,
                TienCoc = tienCoc,
                IdNhanVienXuLy = staffUserId,
                ThoiGianTao = now,
                ThoiGianCapNhat = now
            };

            _context.YeuCauThues.Add(rental);

            await _context.SaveChangesAsync();

            await WriteAuditAsync(
                staffUserId,
                rental,
                "RENTAL_CREATED_OFFLINE");

            await transaction.CommitAsync();

            return MapToResponseDto(rental);
        }

        #endregion


        #region Get Rentals

        public async Task<PagedResult<RentalResponseDto>>
            GetRentalsAsync(
                RentalFilterDto filter,
                int currentUserId,
                string? currentUserRole)
        {
            var query =
                _context.YeuCauThues
                    .AsNoTracking()
                    .AsQueryable();

            if (string.Equals(
                    currentUserRole,
                    "KHACH_HANG",
                    StringComparison.OrdinalIgnoreCase))
            {
                var customer =
                    await GetCustomerByUserIdAsync(
                        currentUserId);

                query = query.Where(
                    r => r.IdKhachHang == customer.Id);
            }
            else if (filter.IdKhachHang.HasValue)
            {
                query = query.Where(
                    r =>
                        r.IdKhachHang ==
                        filter.IdKhachHang.Value);
            }

            if (!string.IsNullOrEmpty(
                    filter.TrangThai))
            {
                query = query.Where(
                    r =>
                        r.TrangThai ==
                        filter.TrangThai);
            }

            if (!string.IsNullOrEmpty(filter.Nguon))
            {
                query = query.Where(
                    r => r.Nguon == filter.Nguon);
            }

            if (filter.IdXe.HasValue)
            {
                query = query.Where(
                    r =>
                        r.IdXe ==
                        filter.IdXe.Value);
            }

            if (filter.FromDate.HasValue)
            {
                query = query.Where(
                    r =>
                        r.ThoiGianNhan >=
                        filter.FromDate.Value);
            }

            if (filter.ToDate.HasValue)
            {
                query = query.Where(
                    r =>
                        r.ThoiGianTraDuKien <=
                        filter.ToDate.Value);
            }

            int totalCount =
                await query.CountAsync();

            int page =
                filter.Page > 0
                    ? filter.Page
                    : 1;

            int pageSize =
                filter.PageSize > 0
                    ? filter.PageSize
                    : 10;

            var rentals =
                await query
                    .OrderByDescending(
                        r => r.ThoiGianTao)
                    .Skip(
                        (page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

            var items =
                rentals
                    .Select(MapToResponseDto)
                    .ToList();

            return new PagedResult<RentalResponseDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        #endregion


        #region Get Rental By Id

        public async Task<RentalDetailDto?>
            GetRentalByIdAsync(
                int id,
                int currentUserId,
                string? currentUserRole)
        {
            var rental =
                await _context.YeuCauThues
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        r => r.Id == id);

            if (rental == null)
            {
                return null;
            }

            if (string.Equals(
                    currentUserRole,
                    "KHACH_HANG",
                    StringComparison.OrdinalIgnoreCase))
            {
                var customer =
                    await GetCustomerByUserIdAsync(
                        currentUserId);

                if (rental.IdKhachHang != customer.Id)
                {
                    throw new UnauthorizedAccessException(
                        "Bạn không có quyền truy cập thông tin yêu cầu thuê này.");
                }
            }

            return new RentalDetailDto
            {
                Id = rental.Id,
                IdKhachHang = rental.IdKhachHang,
                IdXe = rental.IdXe,
                Nguon = rental.Nguon,
                ThoiGianNhan =
                    rental.ThoiGianNhan,
                ThoiGianTraDuKien =
                    rental.ThoiGianTraDuKien,
                TrangThai = rental.TrangThai,
                TienUocTinh =
                    rental.TienUocTinh,
                TienCoc = rental.TienCoc,
                HinhThucNhanXe = rental.HinhThucNhanXe,
                TenNguoiNhan = rental.TenNguoiNhan,
                SoDienThoaiNhan = rental.SoDienThoaiNhan,
                DiaChiGiaoXe = rental.DiaChiGiaoXe,
                GhiChuGiaoXe = rental.GhiChuGiaoXe,
                LyDoTuChoi =
                    rental.LyDoTuChoi,
                LyDoHuy = rental.LyDoHuy,
                IdNhanVienXuLy =
                    rental.IdNhanVienXuLy,
                ThoiGianTao =
                    rental.ThoiGianTao,
                ThoiGianCapNhat =
                    rental.ThoiGianCapNhat
            };
        }

        #endregion


        #region Approve Rental

        public async Task<RentalResponseDto> ApproveRentalAsync(
            int rentalId,
            int staffUserId,
            string? currentUserRole)
        {
            ValidateStaffRole(currentUserRole);

            var rental =
                await _context.YeuCauThues
                    .FindAsync(rentalId);

            if (rental == null)
            {
                throw new KeyNotFoundException(
                    "Không tìm thấy yêu cầu thuê.");
            }

            if (rental.TrangThai != "PENDING")
            {
                throw new InvalidOperationException(
                    "Chỉ có thể chấp nhận yêu cầu đang ở trạng thái PENDING.");
            }

            rental.TrangThai = "APPROVED";
            rental.IdNhanVienXuLy = staffUserId;
            rental.ThoiGianCapNhat = DateTime.Now;

            await _context.SaveChangesAsync();

            await WriteAuditAsync(
                staffUserId,
                rental,
                "RENTAL_APPROVED");

            return MapToResponseDto(rental);
        }

        #endregion


        #region Reject Rental

        public async Task<RentalResponseDto> RejectRentalAsync(
            int rentalId,
            RejectRentalDto request,
            int staffUserId,
            string? currentUserRole)
        {
            ValidateStaffRole(currentUserRole);

            var rental =
                await _context.YeuCauThues
                    .FindAsync(rentalId);

            if (rental == null)
            {
                throw new KeyNotFoundException(
                    "Không tìm thấy yêu cầu thuê.");
            }

            if (rental.TrangThai != "PENDING")
            {
                throw new InvalidOperationException(
                    "Chỉ có thể từ chối yêu cầu đang ở trạng thái PENDING.");
            }

            if (string.IsNullOrWhiteSpace(
                    request.LyDoTuChoi))
            {
                throw new ArgumentException(
                    "Lý do từ chối không được để trống.");
            }

            rental.TrangThai = "REJECTED";
            rental.LyDoTuChoi =
                request.LyDoTuChoi;
            rental.IdNhanVienXuLy =
                staffUserId;
            rental.ThoiGianCapNhat =
                DateTime.Now;

            await _context.SaveChangesAsync();

            await WriteAuditAsync(
                staffUserId,
                rental,
                "RENTAL_REJECTED");

            return MapToResponseDto(rental);
        }

        #endregion


        #region Cancel Rental

        public async Task<RentalResponseDto> CancelRentalAsync(
            int rentalId,
            CancelRentalDto request,
            int staffUserId,
            string? currentUserRole)
        {
            ValidateStaffRole(currentUserRole);

            var rental =
                await _context.YeuCauThues
                    .FindAsync(rentalId);

            if (rental == null)
            {
                throw new KeyNotFoundException(
                    "Không tìm thấy yêu cầu thuê.");
            }

            if (rental.TrangThai != "PENDING" &&
                rental.TrangThai != "APPROVED")
            {
                throw new InvalidOperationException(
                    "Chỉ có thể hủy yêu cầu đang ở trạng thái PENDING hoặc APPROVED.");
            }

            if (string.IsNullOrWhiteSpace(
                    request.LyDoHuy))
            {
                throw new ArgumentException(
                    "Lý do hủy không được để trống.");
            }

            rental.TrangThai = "CANCELLED";
            rental.LyDoHuy = request.LyDoHuy;
            rental.IdNhanVienXuLy =
                staffUserId;
            rental.ThoiGianCapNhat =
                DateTime.Now;

            await _context.SaveChangesAsync();

            await WriteAuditAsync(
                staffUserId,
                rental,
                "RENTAL_CANCELLED");

            return MapToResponseDto(rental);
        }

        #endregion


        #region Availability

        public async Task<bool> CheckRentalOverlapAsync(
            int idXe,
            DateTime thoiGianNhan,
            DateTime thoiGianTraDuKien)
        {
            ValidateRentalDates(
                thoiGianNhan,
                thoiGianTraDuKien);

            return await _context.YeuCauThues
                .AnyAsync(r =>
                    r.IdXe == idXe &&
                    r.TrangThai != "REJECTED" &&
                    r.TrangThai != "CANCELLED" &&
                    r.ThoiGianNhan <
                        thoiGianTraDuKien &&
                    r.ThoiGianTraDuKien >
                        thoiGianNhan);
        }

        /// <summary>
        /// Lấy toàn bộ khoảng thời gian đang chiếm lịch của một xe.
        /// REJECTED/CANCELLED không chiếm lịch.
        /// Có thể giới hạn theo khoảng from/to.
        /// </summary>
        public async Task<List<BookedPeriodDto>>
            GetBookedPeriodsAsync(
                int idXe,
                DateTime? from = null,
                DateTime? to = null)
        {
            var xeExists =
                await _context.Xes
                    .AsNoTracking()
                    .AnyAsync(x => x.Id == idXe);

            if (!xeExists)
            {
                throw new KeyNotFoundException(
                    "Không tìm thấy thông tin xe.");
            }

            if (from.HasValue &&
                to.HasValue &&
                to.Value <= from.Value)
            {
                throw new ArgumentException(
                    "Thời gian kết thúc phải lớn hơn thời gian bắt đầu.");
            }

            var query =
                _context.YeuCauThues
                    .AsNoTracking()
                    .Where(r =>
                        r.IdXe == idXe &&
                        r.TrangThai != "REJECTED" &&
                        r.TrangThai != "CANCELLED");

            // Lấy các booking có giao với khoảng
            // thời gian đang hiển thị trên calendar.
            if (from.HasValue)
            {
                query = query.Where(
                    r =>
                        r.ThoiGianTraDuKien >
                        from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(
                    r =>
                        r.ThoiGianNhan <
                        to.Value);
            }

            return await query
                .OrderBy(r => r.ThoiGianNhan)
                .Select(r =>
                    new BookedPeriodDto
                    {
                        RentalId = r.Id,
                        Start =
                            r.ThoiGianNhan,
                        End =
                            r.ThoiGianTraDuKien,
                        TrangThai =
                            r.TrangThai
                    })
                .ToListAsync();
        }

        #endregion


        #region Audit

        private async Task WriteAuditAsync(
            int? userId,
            YeuCauThue rental,
            string action)
        {
            await _auditLogService.CreateAsync(
                new CreateAuditLogRequest
                {
                    IdNguoiDung = userId,
                    HanhDong = action,
                    LoaiDoiTuong = "RENTAL",
                    IdDoiTuong = rental.Id,
                    MoTa =
                        $"Yêu cầu thuê #{rental.Id} - trạng thái {rental.TrangThai}"
                });
        }

        #endregion


        #region Mapping

        private static RentalResponseDto MapToResponseDto(
            YeuCauThue rental)
        {
            return new RentalResponseDto
            {
                Id = rental.Id,
                IdKhachHang =
                    rental.IdKhachHang,
                IdXe = rental.IdXe,
                Nguon = rental.Nguon,
                ThoiGianNhan =
                    rental.ThoiGianNhan,
                ThoiGianTraDuKien =
                    rental.ThoiGianTraDuKien,
                TrangThai =
                    rental.TrangThai,
                TienUocTinh =
                    rental.TienUocTinh,
                TienCoc =
                    rental.TienCoc,
                HinhThucNhanXe = rental.HinhThucNhanXe,
                TenNguoiNhan = rental.TenNguoiNhan,
                SoDienThoaiNhan = rental.SoDienThoaiNhan,
                DiaChiGiaoXe = rental.DiaChiGiaoXe,
                GhiChuGiaoXe = rental.GhiChuGiaoXe,
                LyDoTuChoi =
                    rental.LyDoTuChoi,
                LyDoHuy =
                    rental.LyDoHuy,
                IdNhanVienXuLy =
                    rental.IdNhanVienXuLy,
                ThoiGianTao =
                    rental.ThoiGianTao,
                ThoiGianCapNhat =
                    rental.ThoiGianCapNhat
            };
        }

        #endregion
    }
}