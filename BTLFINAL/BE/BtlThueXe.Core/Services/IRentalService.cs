using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BtlThueXe.Core.DTOs.Common;
using BtlThueXe.Core.DTOs.Rentals;

namespace BtlThueXe.Core.Services
{
    public interface IRentalService
    {
        Task<RentalResponseDto> CreateOnlineRentalAsync(
            CreateRentalRequestDto request,
            int currentUserId);

        Task<RentalResponseDto> CreateOfflineRentalAsync(
            CreateOfflineRentalRequestDto request,
            int staffUserId,
            string? currentUserRole);

        Task<PagedResult<RentalResponseDto>> GetRentalsAsync(
            RentalFilterDto filter,
            int currentUserId,
            string? currentUserRole);

        Task<RentalDetailDto?> GetRentalByIdAsync(
            int id,
            int currentUserId,
            string? currentUserRole);

        Task<RentalResponseDto> ApproveRentalAsync(
            int rentalId,
            int staffUserId,
            string? currentUserRole);

        Task<RentalResponseDto> RejectRentalAsync(
            int rentalId,
            RejectRentalDto request,
            int staffUserId,
            string? currentUserRole);

        Task<RentalResponseDto> CancelRentalAsync(
            int rentalId,
            CancelRentalDto request,
            int staffUserId,
            string? currentUserRole);

        // Kiểm tra xe có bị trùng lịch trong một khoảng thời gian hay không
        Task<bool> CheckRentalOverlapAsync(
            int idXe,
            DateTime thoiGianNhan,
            DateTime thoiGianTraDuKien);

        // Lấy các khoảng thời gian xe đã có lịch thuê
        Task<List<BookedPeriodDto>> GetBookedPeriodsAsync(
            int idXe,
            DateTime? from = null,
            DateTime? to = null);
    }
}