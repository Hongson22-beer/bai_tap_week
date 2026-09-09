using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BtlThueXe.Core.DTOs.Contracts;
using BtlThueXe.Infrastructure;
using BtlThueXe.Core.Services;
using BtlThueXe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BtlThueXe.Infrastructure.Services
{
    public class ContractService : IContractService
    {
        private readonly ApplicationDbContext _context;

        public ContractService(ApplicationDbContext context)
        {
            _context = context;
        }

        #region 1. CreateContractAsync

        public async Task<ContractResponseDto> CreateContractAsync(
            CreateContractRequestDto request,
            int staffUserId,
            string? currentUserRole)
        {
           // Đổi từ: ValidateRole(currentUserRole, "NHAN_VIEN");
ValidateRole(currentUserRole, "NHAN_VIEN", "Staff", "Admin");

            var yeuCauThue = await _context.YeuCauThues
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == request.IdYeuCauThue);

            if (yeuCauThue == null)
            {
                throw new KeyNotFoundException(
                    $"Không tìm thấy yêu cầu thuê có ID = {request.IdYeuCauThue}.");
            }

            // Chỉ Staff được tạo hợp đồng từ yêu cầu đã APPROVED
            if (!string.Equals(
                    yeuCauThue.TrangThai,
                    "APPROVED",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Chỉ có thể tạo hợp đồng cho yêu cầu thuê đã được phê duyệt (APPROVED).");
            }

            // Mỗi yêu cầu thuê chỉ có tối đa một hợp đồng
            bool isContractExists = await _context.HopDongs
                .AnyAsync(x => x.IdYeuCauThue == request.IdYeuCauThue);

            if (isContractExists)
            {
                throw new InvalidOperationException(
                    $"Yêu cầu thuê ID = {request.IdYeuCauThue} đã tồn tại hợp đồng.");
            }

            // Số hợp đồng phải duy nhất
            bool isSoHopDongExists = await _context.HopDongs
                .AnyAsync(x => x.SoHopDong == request.SoHopDong);

            if (isSoHopDongExists)
            {
                throw new InvalidOperationException(
                    $"Số hợp đồng '{request.SoHopDong}' đã tồn tại trong hệ thống.");
            }

            // Kiểm tra thời gian
            if (request.ThoiGianTraDuKien <= request.ThoiGianNhanDuKien)
            {
                throw new InvalidOperationException(
                    "Thời gian trả dự kiến phải lớn hơn thời gian nhận dự kiến.");
            }

            if (request.DonGiaNgay < 0)
            {
                throw new InvalidOperationException(
                    "Đơn giá ngày không được nhỏ hơn 0.");
            }

            // Tính số ngày thuê theo cách làm tròn lên
            double totalHours =
                (request.ThoiGianTraDuKien - request.ThoiGianNhanDuKien)
                .TotalHours;

            int soNgayThue = (int)Math.Ceiling(totalHours / 24.0);

            if (soNgayThue <= 0)
            {
                soNgayThue = 1;
            }

            // Tính tiền thuê
            decimal tienThue = request.DonGiaNgay * soNgayThue;

            // Tiền cọc lấy từ yêu cầu thuê
            decimal tienCoc = yeuCauThue.TienCoc;

            // Giữ nguyên theo logic hiện tại:
            // tiền cọc là khoản riêng, không cộng vào tiền thuê.
            decimal tongTien = tienThue;

            var now = DateTime.UtcNow;

            var hopDong = new HopDong
            {
                IdYeuCauThue = request.IdYeuCauThue,
                SoHopDong = request.SoHopDong,
                IdNhanVienLap = staffUserId,

                DonGiaNgay = request.DonGiaNgay,
                SoNgayThue = soNgayThue,
                TienThue = tienThue,
                TienCoc = tienCoc,
                TongTien = tongTien,

                DieuKhoan = request.DieuKhoan,

                // Trạng thái ban đầu
                TrangThai = "DRAFT",

                ThoiGianNhanDuKien = request.ThoiGianNhanDuKien,
                ThoiGianTraDuKien = request.ThoiGianTraDuKien,

                ThoiGianTao = now,
                ThoiGianCapNhat = now
            };

            // Ghi lịch sử trạng thái đầu tiên
            var history = new LichSuTrangThaiHopDong
            {
                // SỬA TẠI ĐÂY: Thay IdHopDong = hopDong.Id bằng HopDong = hopDong
                IdHopDongNavigation = hopDong,

                TrangThaiCu = null,
                TrangThaiMoi = "DRAFT",

                IdNguoiThayDoi = staffUserId,

                LyDo = "Khởi tạo hợp đồng dạng nháp (DRAFT).",

                ThoiGianThayDoi = now
            };

            // Lưu contract + history trong cùng một lần SaveChanges
            _context.HopDongs.Add(hopDong);
            _context.LichSuTrangThaiHopDongs.Add(history);

            await _context.SaveChangesAsync();

            return MapToResponseDto(hopDong);
        }

        #endregion


        #region 2. GetContractByIdAsync

        public async Task<ContractDetailDto?> GetContractByIdAsync(
            int id,
            int currentUserId,
            string? currentUserRole)
        {
            var hopDong = await _context.HopDongs
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (hopDong == null)
            {
                return null;
            }

            // Customer chỉ được xem hợp đồng của chính mình
            if (IsCustomer(currentUserRole))
            {
                await ValidateCustomerOwnershipAsync(
                    hopDong.IdYeuCauThue,
                    currentUserId);
            }

            return MapToDetailDto(hopDong);
        }

        #endregion


        #region 3. SendContractAsync

        public async Task<ContractResponseDto> SendContractAsync(
            int contractId,
            int staffUserId,
            string? currentUserRole)
        {
            ValidateRole(currentUserRole, "NHAN_VIEN");

            var hopDong = await _context.HopDongs
                .FirstOrDefaultAsync(x => x.Id == contractId);

            if (hopDong == null)
            {
                throw new KeyNotFoundException(
                    $"Không tìm thấy hợp đồng có ID = {contractId}.");
            }

            // Chỉ DRAFT mới được gửi
            if (!string.Equals(
                    hopDong.TrangThai,
                    "DRAFT",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Không thể gửi hợp đồng đang ở trạng thái '{hopDong.TrangThai}'. " +
                    "Trạng thái yêu cầu: 'DRAFT'.");
            }

            var now = DateTime.UtcNow;
            string trangThaiCu = hopDong.TrangThai;

            hopDong.TrangThai = "SENT";
            hopDong.ThoiGianCapNhat = now;

            var history = new LichSuTrangThaiHopDong
            {
                IdHopDong = hopDong.Id,

                TrangThaiCu = trangThaiCu,
                TrangThaiMoi = "SENT",

                IdNguoiThayDoi = staffUserId,

                LyDo = "Gửi hợp đồng cho khách hàng.",

                ThoiGianThayDoi = now
            };

            _context.LichSuTrangThaiHopDongs.Add(history);

            await _context.SaveChangesAsync();

            return MapToResponseDto(hopDong);
        }

        #endregion


        #region 4. ConfirmContractAsync

        public async Task<ContractResponseDto> ConfirmContractAsync(
            int contractId,
            int currentUserId,
            string? currentUserRole)
        {
            ValidateRole(currentUserRole, "KHACH_HANG", "Customer");

            var hopDong = await _context.HopDongs
                .FirstOrDefaultAsync(x => x.Id == contractId);

            if (hopDong == null)
            {
                throw new KeyNotFoundException(
                    $"Không tìm thấy hợp đồng có ID = {contractId}.");
            }

            // Customer chỉ được xác nhận hợp đồng của chính mình
            await ValidateCustomerOwnershipAsync(
                hopDong.IdYeuCauThue,
                currentUserId);

            // Chỉ SENT mới được xác nhận
            if (!string.Equals(
                    hopDong.TrangThai,
                    "SENT",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Không thể xác nhận hợp đồng đang ở trạng thái '{hopDong.TrangThai}'. " +
                    "Trạng thái yêu cầu: 'SENT'.");
            }

            var now = DateTime.UtcNow;
            string trangThaiCu = hopDong.TrangThai;

            hopDong.TrangThai = "CUSTOMER_CONFIRMED";
            hopDong.ThoiGianCapNhat = now;

            var history = new LichSuTrangThaiHopDong
            {
                IdHopDong = hopDong.Id,

                TrangThaiCu = trangThaiCu,
                TrangThaiMoi = "CUSTOMER_CONFIRMED",

                IdNguoiThayDoi = currentUserId,

                LyDo = "Khách hàng đã xác nhận hợp đồng.",

                ThoiGianThayDoi = now
            };

            _context.LichSuTrangThaiHopDongs.Add(history);

            await _context.SaveChangesAsync();

            return MapToResponseDto(hopDong);
        }

        #endregion


        #region 5. RejectContractAsync

        public async Task<ContractResponseDto> RejectContractAsync(
            int contractId,
            RejectContractRequestDto request,
            int currentUserId,
            string? currentUserRole)
        {
            ValidateRole(currentUserRole, "KHACH_HANG");

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.LyDo))
            {
                throw new InvalidOperationException(
                    "Lý do từ chối không được để trống.");
            }

            var hopDong = await _context.HopDongs
                .FirstOrDefaultAsync(x => x.Id == contractId);

            if (hopDong == null)
            {
                throw new KeyNotFoundException(
                    $"Không tìm thấy hợp đồng có ID = {contractId}.");
            }

            // Customer chỉ được từ chối hợp đồng của chính mình
            await ValidateCustomerOwnershipAsync(
                hopDong.IdYeuCauThue,
                currentUserId);

            // Chỉ SENT mới được từ chối
            if (!string.Equals(
                    hopDong.TrangThai,
                    "SENT",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Không thể từ chối hợp đồng đang ở trạng thái '{hopDong.TrangThai}'. " +
                    "Trạng thái yêu cầu: 'SENT'.");
            }

            var now = DateTime.UtcNow;
            string trangThaiCu = hopDong.TrangThai;

            hopDong.TrangThai = "CUSTOMER_REJECTED";
            hopDong.ThoiGianCapNhat = now;

            var history = new LichSuTrangThaiHopDong
            {
                IdHopDong = hopDong.Id,

                TrangThaiCu = trangThaiCu,
                TrangThaiMoi = "CUSTOMER_REJECTED",

                IdNguoiThayDoi = currentUserId,

                LyDo = request.LyDo.Trim(),

                ThoiGianThayDoi = now
            };

            _context.LichSuTrangThaiHopDongs.Add(history);

            await _context.SaveChangesAsync();

            return MapToResponseDto(hopDong);
        }

        #endregion


        #region 6. GetContractHistoryAsync

        public async Task<List<ContractHistoryDto>> GetContractHistoryAsync(
            int contractId,
            int currentUserId,
            string? currentUserRole)
        {
            var hopDong = await _context.HopDongs
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == contractId);

            if (hopDong == null)
            {
                throw new KeyNotFoundException(
                    $"Không tìm thấy hợp đồng có ID = {contractId}.");
            }

            // Customer chỉ được xem lịch sử hợp đồng của chính mình
            if (IsCustomer(currentUserRole))
            {
                await ValidateCustomerOwnershipAsync(
                    hopDong.IdYeuCauThue,
                    currentUserId);
            }

            var histories = await _context.LichSuTrangThaiHopDongs
                .AsNoTracking()
                .Where(x => x.IdHopDong == contractId)
                .OrderBy(x => x.ThoiGianThayDoi)
                .ThenBy(x => x.Id)
                .ToListAsync();

            return histories
                .Select(MapToHistoryDto)
                .ToList();
        }

        #endregion


        #region Helper Methods

        private static void ValidateRole(
    string? currentUserRole,
    params string[] allowedRoles)
{
    if (string.IsNullOrWhiteSpace(currentUserRole) ||
        !allowedRoles.Any(r => r.Equals(currentUserRole, StringComparison.OrdinalIgnoreCase)))
    {
        var rolesText = string.Join(" hoặc ", allowedRoles);
        throw new UnauthorizedAccessException($"Thao tác yêu cầu quyền: {rolesText}.");
    }
}


        private static bool IsCustomer(string? currentUserRole)
        {
            return !string.IsNullOrWhiteSpace(currentUserRole) &&
                   currentUserRole.Equals(
                       "KHACH_HANG",
                       StringComparison.OrdinalIgnoreCase);
        }


        private async Task ValidateCustomerOwnershipAsync(
            int idYeuCauThue,
            int currentUserId)
        {
            var yeuCauThue = await _context.YeuCauThues
                .AsNoTracking()
                .Include(x => x.IdKhachHangNavigation)
                .FirstOrDefaultAsync(x => x.Id == idYeuCauThue);

            if (yeuCauThue == null ||
                yeuCauThue.IdKhachHangNavigation == null ||
                yeuCauThue.IdKhachHangNavigation.IdNguoiDung != currentUserId)
            {
                throw new UnauthorizedAccessException(
                    "Bạn không có quyền truy cập hoặc thao tác trên hợp đồng này.");
            }
        }


        private static ContractResponseDto MapToResponseDto(
            HopDong entity)
        {
            return new ContractResponseDto
            {
                Id = entity.Id,
                IdYeuCauThue = entity.IdYeuCauThue,
                SoHopDong = entity.SoHopDong,
                IdNhanVienLap = entity.IdNhanVienLap,

                DonGiaNgay = entity.DonGiaNgay,
                SoNgayThue = entity.SoNgayThue,
                TienThue = entity.TienThue,
                TienCoc = entity.TienCoc,
                TongTien = entity.TongTien,

                DieuKhoan = entity.DieuKhoan,
                TrangThai = entity.TrangThai,

                ThoiGianNhanDuKien = entity.ThoiGianNhanDuKien,
                ThoiGianTraDuKien = entity.ThoiGianTraDuKien,

                ThoiGianNhanThucTe = entity.ThoiGianNhanThucTe,
                ThoiGianTraThucTe = entity.ThoiGianTraThucTe,

                ThoiGianTao = entity.ThoiGianTao,
                ThoiGianCapNhat = entity.ThoiGianCapNhat
            };
        }


        private static ContractDetailDto MapToDetailDto(
            HopDong entity)
        {
            return new ContractDetailDto
            {
                Id = entity.Id,
                IdYeuCauThue = entity.IdYeuCauThue,
                SoHopDong = entity.SoHopDong,
                IdNhanVienLap = entity.IdNhanVienLap,

                DonGiaNgay = entity.DonGiaNgay,
                SoNgayThue = entity.SoNgayThue,
                TienThue = entity.TienThue,
                TienCoc = entity.TienCoc,
                TongTien = entity.TongTien,

                DieuKhoan = entity.DieuKhoan,
                TrangThai = entity.TrangThai,

                ThoiGianNhanDuKien = entity.ThoiGianNhanDuKien,
                ThoiGianTraDuKien = entity.ThoiGianTraDuKien,

                ThoiGianNhanThucTe = entity.ThoiGianNhanThucTe,
                ThoiGianTraThucTe = entity.ThoiGianTraThucTe,

                ThoiGianTao = entity.ThoiGianTao,
                ThoiGianCapNhat = entity.ThoiGianCapNhat
            };
        }


        private static ContractHistoryDto MapToHistoryDto(
            LichSuTrangThaiHopDong entity)
        {
            return new ContractHistoryDto
            {
                Id = entity.Id,
                IdHopDong = entity.IdHopDong,
                TrangThaiCu = entity.TrangThaiCu,
                TrangThaiMoi = entity.TrangThaiMoi,
                IdNguoiThayDoi = entity.IdNguoiThayDoi,
                LyDo = entity.LyDo,
                ThoiGianThayDoi = entity.ThoiGianThayDoi
            };
        }

        #endregion
    }
}