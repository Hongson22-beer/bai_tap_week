using System.Security.Claims;
using BtlThueXe.Core.DTOs.Cancellations;
using BtlThueXe.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BtlThueXe.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CancellationsController : ControllerBase
{
    private readonly ICancellationService _cancellationService;

    public CancellationsController(
        ICancellationService cancellationService)
    {
        _cancellationService = cancellationService;
    }

    // =========================================================
    // LẤY USER ID TỪ JWT
    // =========================================================
    private int GetCurrentUserId()
    {
        var userIdClaim =
            User.FindFirst(ClaimTypes.NameIdentifier)
            ?? User.FindFirst("sub")
            ?? User.FindFirst("id");

        if (userIdClaim == null ||
            !int.TryParse(userIdClaim.Value, out int userId))
        {
            throw new UnauthorizedAccessException(
                "Không xác định được người dùng từ JWT.");
        }

        return userId;
    }

    // =========================================================
    // POST /api/cancellations
    // KHÁCH HÀNG TẠO YÊU CẦU HỦY
    // =========================================================
    [HttpPost]
    [Authorize(Roles = "KHACH_HANG")]
    public async Task<IActionResult> Create(
        [FromBody] CreateCancellationRequest request)
    {
        try
        {
            int currentUserId = GetCurrentUserId();

            // Không sử dụng IdNguoiYeuCau do client gửi
            // Service sử dụng user ID lấy từ JWT.
            var result =
                await _cancellationService.CreateAsync(
                    request,
                    currentUserId);

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.Id },
                result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new
                {
                    message = ex.Message
                });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new
            {
                message = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new
            {
                message = ex.Message
            });
        }
    }

    // =========================================================
    // PUT /api/cancellations/{id}/process
    // NHÂN VIÊN DUYỆT / TỪ CHỐI
    // =========================================================
    [HttpPut("{id:int}/process")]
    [Authorize(Roles = "NHAN_VIEN")]
    public async Task<IActionResult> Process(
        int id,
        [FromBody] ProcessCancellationRequest request)
    {
        try
        {
            if (id <= 0)
            {
                return BadRequest(new
                {
                    message =
                        "ID yêu cầu hủy không hợp lệ."
                });
            }

            int currentUserId =
                GetCurrentUserId();

            // Không sử dụng IdNguoiXuLy do client gửi.
            var result =
                await _cancellationService.ProcessAsync(
                    id,
                    request,
                    currentUserId);

            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new
            {
                message = ex.Message
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new
            {
                message = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new
            {
                message = ex.Message
            });
        }
    }

    // =========================================================
    // GET /api/cancellations/{id}
    // NHÂN VIÊN / ADMIN
    // =========================================================
    [HttpGet("{id:int}")]
    [Authorize(Roles = "NHAN_VIEN,ADMIN")]
    public async Task<IActionResult> GetById(int id)
    {
        if (id <= 0)
        {
            return BadRequest(new
            {
                message =
                    "ID yêu cầu hủy không hợp lệ."
            });
        }

        var result =
            await _cancellationService.GetByIdAsync(id);

        if (result == null)
        {
            return NotFound(new
            {
                message =
                    "Không tìm thấy yêu cầu hủy hợp đồng."
            });
        }

        return Ok(result);
    }

    // =========================================================
    // GET /api/cancellations/my/contract/{idHopDong}
    // KHÁCH HÀNG XEM YÊU CẦU HỦY CỦA CHÍNH MÌNH
    // =========================================================
    [HttpGet("my/contract/{idHopDong:int}")]
    [Authorize(Roles = "KHACH_HANG")]
    public async Task<IActionResult> GetMyByContract(int idHopDong)
    {
        try
        {
            var result = await _cancellationService.GetMyByContractIdAsync(
                idHopDong, GetCurrentUserId());
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    // =========================================================
    // GET /api/cancellations/contract/{idHopDong}
    // NHÂN VIÊN / ADMIN
    // =========================================================
    [HttpGet("contract/{idHopDong:int}")]
    [Authorize(Roles = "NHAN_VIEN,ADMIN")]
    public async Task<IActionResult> GetByContract(
        int idHopDong)
    {
        if (idHopDong <= 0)
        {
            return BadRequest(new
            {
                message =
                    "ID hợp đồng không hợp lệ."
            });
        }

        var result =
            await _cancellationService
                .GetByContractIdAsync(idHopDong);

        return Ok(result);
    }
}