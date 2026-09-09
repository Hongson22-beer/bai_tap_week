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

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                throw new UnauthorizedAccessException("Không thể xác định thông tin người dùng từ token.");
            }

            return userId;
        }

        private string? GetCurrentUserRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value
                ?? User.FindFirst("role")?.Value;
        }

        #endregion

        #region 1. POST /api/rentals/online

        [HttpPost("ONLINE")]
        [Authorize(Roles = "KHACH_HANG")]
        public async Task<IActionResult> CreateOnlineRental([FromBody] CreateRentalRequestDto request)
        {
            int currentUserId = GetCurrentUserId();

            var result = await _rentalService.CreateOnlineRentalAsync(request, currentUserId);

            return CreatedAtAction(
                nameof(GetRentalById),
                new { id = result.Id },
                result);
        }

        #endregion

        #region 2. POST /api/rentals/offline

        [HttpPost("offline")]
        [Authorize(Roles = "NHAN_VIEN")]
        public async Task<IActionResult> CreateOfflineRental([FromBody] CreateOfflineRentalRequestDto request)
        {
            int staffUserId = GetCurrentUserId();
            string? currentUserRole = GetCurrentUserRole();

            var result = await _rentalService.CreateOfflineRentalAsync(request, staffUserId, currentUserRole);

            return CreatedAtAction(
                nameof(GetRentalById),
                new { id = result.Id },
                result);
        }

        #endregion

        #region 3. GET /api/rentals

        [HttpGet]
        [Authorize(Roles = "NHAN_VIEN")]
        public async Task<ActionResult<PagedResult<RentalResponseDto>>> GetRentals([FromQuery] RentalFilterDto filter)
        {
            int currentUserId = GetCurrentUserId();
            string? currentUserRole = GetCurrentUserRole();

            var result = await _rentalService.GetRentalsAsync(filter, currentUserId, currentUserRole);

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

            var result = await _rentalService.GetRentalByIdAsync(id, currentUserId, currentUserRole);

            if (result == null)
            {
                return NotFound(new { message = $"Không tìm thấy yêu cầu thuê có ID = {id}." });
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

            var result = await _rentalService.ApproveRentalAsync(id, staffUserId, currentUserRole);

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

            var result = await _rentalService.RejectRentalAsync(id, request, staffUserId, currentUserRole);

            return Ok(result);
        }

        #endregion
    }
}