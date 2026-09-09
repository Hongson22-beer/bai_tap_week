using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BtlThueXe.Core.DTOs.Common;
using BtlThueXe.Core.DTOs.Rentals;
using BtlThueXe.Core.Services;
using BtlThueXe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BtlThueXe.Infrastructure.Services
{
    public class RentalService : IRentalService
    {
        private readonly ApplicationDbContext _context;

        public RentalService(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Helper Methods - Ownership & Role Validations
        private static void ValidateStaffRole(string? role)
        {
            if (!string.Equals(role, "NHAN_VIEN", StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("Thao tác này chỉ dành riêng cho Nhân viên (NHAN_VIEN).");
            }
        }

        private async Task<KhachHang> GetCustomerByUserIdAsync(int userId)
        {
            var customer = await _context.KhachHangs.FirstOrDefaultAsync(k => k.IdNguoiDung == userId);
            if (customer == null)
            {
                throw new KeyNotFoundException("Không tìm thấy thông tin khách hàng tương ứng với tài khoản này.");
            }
            return customer;
        }

        private static void ValidateRentalDates(DateTime thoiGianNhan, DateTime thoiGianTraDuKien)
        {
            if (thoiGianTraDuKien <= thoiGianNhan)
            {
                throw new InvalidOperationException("Thời gian trả dự kiến phải lớn hơn thời gian nhận.");
            }
        }

        private async Task<Xe> GetAndValidateXeAsync(int idXe)
        {
            var xe = await _context.Xes.FindAsync(idXe);
            if (xe == null)
            {
                throw new KeyNotFoundException("Không tìm thấy thông tin xe.");
            }

            if (string.Equals(xe.TrangThai, "MAINTENANCE", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(xe.TrangThai, "INACTIVE", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Xe đang bảo trì hoặc ngưng hoạt động, không thể đặt.");
            }

            return xe;
        }

        private async Task CheckAndThrowIfOverlapAsync(int idXe, DateTime thoiGianNhan, DateTime thoiGianTraDuKien)
        {
            bool isOverlap = await CheckRentalOverlapAsync(idXe, thoiGianNhan, thoiGianTraDuKien);
            if (isOverlap)
            {
                throw new InvalidOperationException("Xe đã có lịch thuê trong khoảng thời gian này.");
            }
        }

        private static (decimal TienUocTinh, decimal TienCoc) CalculateRentalFee(decimal donGiaNgay, DateTime start, DateTime end)
        {
            // Tạm tính số ngày theo quy tắc hiện tại của module.
            // Nếu BA quy định cách tính ngày khác, sẽ thay thế tại đây.
            var totalHours = (end - start).TotalHours;
            int rentalDays = (int)Math.Ceiling(totalHours / 24.0);
            if (rentalDays <= 0) rentalDays = 1;

            decimal tienUocTinh = rentalDays * donGiaNgay;

            // BA chưa quy định công thức tính tiền cọc.
            // Không tự hard-code tỷ lệ phần trăm tại RentalService.
            decimal tienCoc = 0;

            return (tienUocTinh, tienCoc);
        }
        #endregion

        public async Task<RentalResponseDto> CreateOnlineRentalAsync(CreateRentalRequestDto request, int currentUserId)
        {
            ValidateRentalDates(request.ThoiGianNhan, request.ThoiGianTraDuKien);

            var khachHang = await GetCustomerByUserIdAsync(currentUserId);
            if (!khachHang.CccdDaXacMinh)
            {
                throw new InvalidOperationException("Tài khoản chưa xác minh CCCD, không thể tạo yêu cầu thuê ONLINE.");
            }

            var xe = await GetAndValidateXeAsync(request.IdXe);
            await CheckAndThrowIfOverlapAsync(xe.Id, request.ThoiGianNhan, request.ThoiGianTraDuKien);

            var (tienUocTinh, tienCoc) = CalculateRentalFee(xe.DonGiaNgay, request.ThoiGianNhan, request.ThoiGianTraDuKien);
            var now = DateTime.Now;

            var rental = new YeuCauThue
            {
                IdKhachHang = khachHang.Id,
                IdXe = xe.Id,
                Nguon = "ONLINE",
                ThoiGianNhan = request.ThoiGianNhan,
                ThoiGianTraDuKien = request.ThoiGianTraDuKien,
                TrangThai = "PENDING",
                TienUocTinh = tienUocTinh,
                TienCoc = tienCoc,
                ThoiGianTao = now,
                ThoiGianCapNhat = now
            };

            _context.YeuCauThues.Add(rental);
            await _context.SaveChangesAsync();

            return MapToResponseDto(rental);
        }

        public async Task<RentalResponseDto> CreateOfflineRentalAsync(CreateOfflineRentalRequestDto request, int staffUserId, string? currentUserRole)
        {
            ValidateStaffRole(currentUserRole);
            ValidateRentalDates(request.ThoiGianNhan, request.ThoiGianTraDuKien);

            var khachHang = await _context.KhachHangs.FindAsync(request.IdKhachHang);
            if (khachHang == null)
            {
                throw new KeyNotFoundException("Khách hàng không tồn tại.");
            }

            var xe = await GetAndValidateXeAsync(request.IdXe);
            await CheckAndThrowIfOverlapAsync(xe.Id, request.ThoiGianNhan, request.ThoiGianTraDuKien);

            var (tienUocTinh, tienCoc) = CalculateRentalFee(xe.DonGiaNgay, request.ThoiGianNhan, request.ThoiGianTraDuKien);
            var now = DateTime.Now;

            var rental = new YeuCauThue
            {
                IdKhachHang = request.IdKhachHang,
                IdXe = request.IdXe,
                Nguon = "OFFLINE",
                ThoiGianNhan = request.ThoiGianNhan,
                ThoiGianTraDuKien = request.ThoiGianTraDuKien,
                TrangThai = "PENDING",
                TienUocTinh = tienUocTinh,
                TienCoc = tienCoc,
                IdNhanVienXuLy = staffUserId,
                ThoiGianTao = now,
                ThoiGianCapNhat = now
            };

            _context.YeuCauThues.Add(rental);
            await _context.SaveChangesAsync();

            return MapToResponseDto(rental);
        }

        public async Task<PagedResult<RentalResponseDto>> GetRentalsAsync(RentalFilterDto filter, int currentUserId, string? currentUserRole)
        {
            var query = _context.YeuCauThues.AsNoTracking().AsQueryable();

            // Safe string comparison chống NullReferenceException
            if (string.Equals(currentUserRole, "KHACH_HANG", StringComparison.OrdinalIgnoreCase))
            {
                var customer = await GetCustomerByUserIdAsync(currentUserId);
                query = query.Where(r => r.IdKhachHang == customer.Id);
            }
            else if (filter.IdKhachHang.HasValue)
            {
                query = query.Where(r => r.IdKhachHang == filter.IdKhachHang.Value);
            }

            if (!string.IsNullOrEmpty(filter.TrangThai))
            {
                query = query.Where(r => r.TrangThai == filter.TrangThai);
            }

            if (!string.IsNullOrEmpty(filter.Nguon))
            {
                query = query.Where(r => r.Nguon == filter.Nguon);
            }

            if (filter.IdXe.HasValue)
            {
                query = query.Where(r => r.IdXe == filter.IdXe.Value);
            }

            if (filter.FromDate.HasValue)
            {
                query = query.Where(r => r.ThoiGianNhan >= filter.FromDate.Value);
            }

            if (filter.ToDate.HasValue)
            {
                query = query.Where(r => r.ThoiGianTraDuKien <= filter.ToDate.Value);
            }

            int totalCount = await query.CountAsync();

            int page = filter.Page > 0 ? filter.Page : 1;
            int pageSize = filter.PageSize > 0 ? filter.PageSize : 10;

            // Query lấy Entity trước, thực hiện Map sang DTO tại memory để tránh lỗi EF Core Translation
            var rentals = await query
                .OrderByDescending(r => r.ThoiGianTao)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = rentals.Select(MapToResponseDto).ToList();

            return new PagedResult<RentalResponseDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<RentalDetailDto?> GetRentalByIdAsync(int id, int currentUserId, string? currentUserRole)
        {
            var rental = await _context.YeuCauThues
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id);

            if (rental == null) return null;

            if (string.Equals(currentUserRole, "KHACH_HANG", StringComparison.OrdinalIgnoreCase))
            {
                var customer = await GetCustomerByUserIdAsync(currentUserId);
                if (rental.IdKhachHang != customer.Id)
                {
                    throw new UnauthorizedAccessException("Bạn không có quyền truy cập thông tin yêu cầu thuê này.");
                }
            }

            return new RentalDetailDto
            {
                Id = rental.Id,
                IdKhachHang = rental.IdKhachHang,
                IdXe = rental.IdXe,
                Nguon = rental.Nguon,
                ThoiGianNhan = rental.ThoiGianNhan,
                ThoiGianTraDuKien = rental.ThoiGianTraDuKien,
                TrangThai = rental.TrangThai,
                TienUocTinh = rental.TienUocTinh,
                TienCoc = rental.TienCoc,
                LyDoTuChoi = rental.LyDoTuChoi,
                LyDoHuy = rental.LyDoHuy,
                IdNhanVienXuLy = rental.IdNhanVienXuLy,
                ThoiGianTao = rental.ThoiGianTao,
                ThoiGianCapNhat = rental.ThoiGianCapNhat
            };
        }

        public async Task<RentalResponseDto> ApproveRentalAsync(int rentalId, int staffUserId, string? currentUserRole)
        {
            ValidateStaffRole(currentUserRole);

            var rental = await _context.YeuCauThues.FindAsync(rentalId);
            if (rental == null)
            {
                throw new KeyNotFoundException("Không tìm thấy yêu cầu thuê.");
            }

            if (rental.TrangThai != "PENDING")
            {
                throw new InvalidOperationException("Chỉ có thể chấp nhận yêu cầu đang ở trạng thái PENDING.");
            }

            rental.TrangThai = "APPROVED";
            rental.IdNhanVienXuLy = staffUserId;
            rental.ThoiGianCapNhat = DateTime.Now;

            await _context.SaveChangesAsync();
            return MapToResponseDto(rental);
        }

        public async Task<RentalResponseDto> RejectRentalAsync(int rentalId, RejectRentalDto request, int staffUserId, string? currentUserRole)
        {
            ValidateStaffRole(currentUserRole);

            var rental = await _context.YeuCauThues.FindAsync(rentalId);
            if (rental == null)
            {
                throw new KeyNotFoundException("Không tìm thấy yêu cầu thuê.");
            }

            if (rental.TrangThai != "PENDING")
            {
                throw new InvalidOperationException("Chỉ có thể từ chối yêu cầu đang ở trạng thái PENDING.");
            }

            if (string.IsNullOrWhiteSpace(request.LyDoTuChoi))
            {
                throw new ArgumentException("Lý do từ chối không được để trống.");
            }

            rental.TrangThai = "REJECTED";
            rental.LyDoTuChoi = request.LyDoTuChoi;
            rental.IdNhanVienXuLy = staffUserId;
            rental.ThoiGianCapNhat = DateTime.Now;

            await _context.SaveChangesAsync();
            return MapToResponseDto(rental);
        }

        public async Task<RentalResponseDto> CancelRentalAsync(int rentalId, CancelRentalDto request, int staffUserId, string? currentUserRole)
        {
            ValidateStaffRole(currentUserRole);

            var rental = await _context.YeuCauThues.FindAsync(rentalId);
            if (rental == null)
            {
                throw new KeyNotFoundException("Không tìm thấy yêu cầu thuê.");
            }

            if (rental.TrangThai != "PENDING" && rental.TrangThai != "APPROVED")
            {
                throw new InvalidOperationException("Chỉ có thể hủy yêu cầu đang ở trạng thái PENDING hoặc APPROVED.");
            }

            if (string.IsNullOrWhiteSpace(request.LyDoHuy))
            {
                throw new ArgumentException("Lý do hủy không được để trống.");
            }

            rental.TrangThai = "CANCELLED";
            rental.LyDoHuy = request.LyDoHuy;
            rental.IdNhanVienXuLy = staffUserId;
            rental.ThoiGianCapNhat = DateTime.Now;

            await _context.SaveChangesAsync();
            return MapToResponseDto(rental);
        }

        public async Task<bool> CheckRentalOverlapAsync(int idXe, DateTime thoiGianNhan, DateTime thoiGianTraDuKien)
        {
            ValidateRentalDates(thoiGianNhan, thoiGianTraDuKien);

            return await _context.YeuCauThues.AnyAsync(r =>
                r.IdXe == idXe &&
                r.TrangThai != "REJECTED" &&
                r.TrangThai != "CANCELLED" &&
                r.ThoiGianNhan < thoiGianTraDuKien &&
                r.ThoiGianTraDuKien > thoiGianNhan);
        }

        private static RentalResponseDto MapToResponseDto(YeuCauThue rental)
        {
            return new RentalResponseDto
            {
                Id = rental.Id,
                IdKhachHang = rental.IdKhachHang,
                IdXe = rental.IdXe,
                Nguon = rental.Nguon,
                ThoiGianNhan = rental.ThoiGianNhan,
                ThoiGianTraDuKien = rental.ThoiGianTraDuKien,
                TrangThai = rental.TrangThai,
                TienUocTinh = rental.TienUocTinh,
                TienCoc = rental.TienCoc,
                LyDoTuChoi = rental.LyDoTuChoi,
                LyDoHuy = rental.LyDoHuy,
                IdNhanVienXuLy = rental.IdNhanVienXuLy,
                ThoiGianTao = rental.ThoiGianTao,
                ThoiGianCapNhat = rental.ThoiGianCapNhat
            };
        }
    }
}