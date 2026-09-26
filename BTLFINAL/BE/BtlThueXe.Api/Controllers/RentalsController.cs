using System;
using System.Security.Claims;
using System.Threading.Tasks;
using BtlThueXe.Core.DTOs.Common;
using BtlThueXe.Core.DTOs.Rentals;
using BtlThueXe.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BtlThueXe.Api.Controllers
{
    [ApiController]
    [Route("api/rentals")]
    [Authorize]
    public class RentalsController : ControllerBase
    {
        private readonly IRentalService _rentalService;

        public RentalsController(IRentalService rentalService)
        {
            _rentalService = rentalService;
        }

        #region Helper Methods

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                           ?? User.FindFirst("sub")
                           ?? User.FindFirst("id");

            if (userIdClaim == null ||
                !int.TryParse(userIdClaim.Value, out int userId))
            {
                throw new UnauthorizedAccessException(
                    "Không thể xác định thông tin người dùng từ token.");
            }

            return userId;
        }

        private string? GetCurrentUserRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value
                ?? User.FindFirst("role")?.Value;
        }

        #endregion


        #region 1. POST /api/rentals/ONLINE

        [HttpPost("ONLINE")]
        [Authorize(Roles = "KHACH_HANG")]
        public async Task<IActionResult> CreateOnlineRental(
            [FromBody] CreateRentalRequestDto request)
        {
            int currentUserId = GetCurrentUserId();

            var result = await _rentalService.CreateOnlineRentalAsync(
                request,
                currentUserId);

            return CreatedAtAction(
                nameof(GetRentalById),
                new { id = result.Id },
                result);
        }

        #endregion


        #region 2. POST /api/rentals/offline

        [HttpPost("offline")]
        [Authorize(Roles = "NHAN_VIEN")]
        public async Task<IActionResult> CreateOfflineRental(
            [FromBody] CreateOfflineRentalRequestDto request)
        {
            int staffUserId = GetCurrentUserId();
            string? currentUserRole = GetCurrentUserRole();

            var result = await _rentalService.CreateOfflineRentalAsync(
                request,
                staffUserId,
                currentUserRole);

            return CreatedAtAction(
                nameof(GetRentalById),
                new { id = result.Id },
                result);
        }

        #endregion


        #region 3. GET /api/rentals

        [HttpGet]
        [Authorize(Roles = "NHAN_VIEN,KHACH_HANG,ADMIN")]
        public async Task<ActionResult<PagedResult<RentalResponseDto>>> GetRentals(
            [FromQuery] RentalFilterDto filter)
        {
            int currentUserId = GetCurrentUserId();
            string? currentUserRole = GetCurrentUserRole();

            var result = await _rentalService.GetRentalsAsync(
                filter,
                currentUserId,
                currentUserRole);

            return Ok(result);
        }

        #endregion


        #region 3B. GET /api/rentals/my

        [HttpGet("my")]
        [Authorize(Roles = "KHACH_HANG")]
        public async Task<ActionResult<PagedResult<RentalResponseDto>>> GetMyRentals()
        {
            int currentUserId = GetCurrentUserId();
            var filter = new RentalFilterDto { Page = 1, PageSize = 100 };
            var result = await _rentalService.GetRentalsAsync(filter, currentUserId, "KHACH_HANG");
            return Ok(result);
        }

        #endregion


        #region 4. GET /api/rentals/{id}

        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<ActionResult<RentalDetailDto>> GetRentalById(int id)
        {
            int currentUserId = GetCurrentUserId();
            string? currentUserRole = GetCurrentUserRole();

            var result = await _rentalService.GetRentalByIdAsync(
                id,
                currentUserId,
                currentUserRole);

            if (result == null)
            {
                return NotFound(new
                {
                    message = $"Không tìm thấy yêu cầu thuê có ID = {id}."
                });
            }

            return Ok(result);
        }

        #endregion


        #region 5. PUT /api/rentals/{id}/approve

        [HttpPut("{id:int}/approve")]
        [Authorize(Roles = "NHAN_VIEN")]
        public async Task<ActionResult<RentalResponseDto>> ApproveRental(int id)
        {
            int staffUserId = GetCurrentUserId();
            string? currentUserRole = GetCurrentUserRole();

            var result = await _rentalService.ApproveRentalAsync(
                id,
                staffUserId,
                currentUserRole);

            return Ok(result);
        }

        #endregion


        #region 6. PUT /api/rentals/{id}/reject

        [HttpPut("{id:int}/reject")]
        [Authorize(Roles = "NHAN_VIEN")]
        public async Task<ActionResult<RentalResponseDto>> RejectRental(
            int id,
            [FromBody] RejectRentalDto request)
        {
            int staffUserId = GetCurrentUserId();
            string? currentUserRole = GetCurrentUserRole();

            var result = await _rentalService.RejectRentalAsync(
                id,
                request,
                staffUserId,
                currentUserRole);

            return Ok(result);
        }

        #endregion


        #region 7. PUT /api/rentals/{id}/cancel

        [HttpPut("{id:int}/cancel")]
        [Authorize(Roles = "NHAN_VIEN")]
        public async Task<ActionResult<RentalResponseDto>> CancelRental(
            int id,
            [FromBody] CancelRentalDto request)
        {
            int staffUserId = GetCurrentUserId();
            string? currentUserRole = GetCurrentUserRole();

            var result = await _rentalService.CancelRentalAsync(
                id,
                request,
                staffUserId,
                currentUserRole);

            return Ok(result);
        }

        #endregion


        #region 8. GET /api/rentals/availability

        /// <summary>
        /// Kiểm tra xe có khả dụng trong khoảng thời gian khách chọn hay không.
        /// </summary>
        [HttpGet("availability")]
        [Authorize(Roles = "KHACH_HANG")]
        public async Task<IActionResult> CheckAvailability(
            [FromQuery] int idXe,
            [FromQuery] DateTime thoiGianNhan,
            [FromQuery] DateTime thoiGianTraDuKien)
        {
            if (idXe <= 0)
            {
                return BadRequest(new
                {
                    available = false,
                    message = "ID xe không hợp lệ."
                });
            }

            if (thoiGianTraDuKien <= thoiGianNhan)
            {
                return BadRequest(new
                {
                    available = false,
                    message = "Thời gian trả phải sau thời gian nhận."
                });
            }

            bool hasOverlap =
                await _rentalService.CheckRentalOverlapAsync(
                    idXe,
                    thoiGianNhan,
                    thoiGianTraDuKien);

            if (hasOverlap)
            {
                return Ok(new
                {
                    idXe,
                    available = false,
                    message =
                        "Xe đã có lịch thuê trong khoảng thời gian này. " +
                        "Vui lòng chọn thời gian khác."
                });
            }

            return Ok(new
            {
                idXe,
                available = true,
                message =
                    "Xe khả dụng trong khoảng thời gian đã chọn."
            });
        }

        #endregion


        #region 9. GET /api/rentals/vehicle/{idXe}/booked-periods

        /// <summary>
        /// Lấy các khoảng thời gian xe đã có lịch thuê.
        /// Dùng để frontend hiển thị ngày bận trên lịch.
        /// </summary>
        [HttpGet("vehicle/{idXe:int}/booked-periods")]
        [Authorize(Roles = "KHACH_HANG")]
        public async Task<ActionResult> GetBookedPeriods(
            int idXe,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null)
        {
            if (idXe <= 0)
            {
                return BadRequest(new
                {
                    message = "ID xe không hợp lệ."
                });
            }

            if (from.HasValue &&
                to.HasValue &&
                to.Value <= from.Value)
            {
                return BadRequest(new
                {
                    message =
                        "Thời gian kết thúc phải sau thời gian bắt đầu."
                });
            }

            var result =
                await _rentalService.GetBookedPeriodsAsync(
                    idXe,
                    from,
                    to);

            return Ok(result);
        }

        #endregion
    }
}