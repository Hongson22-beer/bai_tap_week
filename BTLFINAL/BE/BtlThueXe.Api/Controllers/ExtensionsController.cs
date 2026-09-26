using System.Security.Claims;
using BtlThueXe.Core.DTOs.Extensions;
using BtlThueXe.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BtlThueXe.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExtensionsController : ControllerBase
{
    private readonly IExtensionService _extensionService;

    public ExtensionsController(
        IExtensionService extensionService)
    {
        _extensionService = extensionService;
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
    // POST /api/extensions
    // KHÁCH HÀNG TẠO YÊU CẦU GIA HẠN
    // =========================================================
    [HttpPost]
    [Authorize(Roles = "KHACH_HANG")]
    public async Task<IActionResult> Create(
        [FromBody] CreateExtensionRequest request)
    {
        try
        {
            int currentUserId = GetCurrentUserId();

            // IdNguoiYeuCau từ body không được tin cậy.
            // Service sử dụng user ID lấy từ JWT.
            var result =
                await _extensionService.CreateAsync(
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
    // PUT /api/extensions/{id}/process
    // NHÂN VIÊN DUYỆT / TỪ CHỐI
    // =========================================================
    [HttpPut("{id:int}/process")]
    [Authorize(Roles = "NHAN_VIEN")]
    public async Task<IActionResult> Process(
        int id,
        [FromBody] ProcessExtensionRequest request)
    {
        try
        {
            if (id <= 0)
            {
                return BadRequest(new
                {
                    message =
                        "ID yêu cầu gia hạn không hợp lệ."
                });
            }

            int currentUserId =
                GetCurrentUserId();

            // IdNguoiXuLy trong body không được tin cậy.
            // Service sử dụng nhân viên từ JWT.
            var result =
                await _extensionService.ProcessAsync(
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
    // GET /api/extensions/{id}
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
                    "ID yêu cầu gia hạn không hợp lệ."
            });
        }

        var result =
            await _extensionService.GetByIdAsync(id);

        if (result == null)
        {
            return NotFound(new
            {
                message =
                    "Không tìm thấy yêu cầu gia hạn."
            });
        }

        return Ok(result);
    }

    // =========================================================
    // GET /api/extensions/contract/{idHopDong}
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
            await _extensionService
                .GetByContractIdAsync(idHopDong);

        return Ok(result);
    }
    // GET /api/extensions/my/contract/{idHopDong}
    // KHÁCH HÀNG xem lịch sử gia hạn của chính mình
    [HttpGet("my/contract/{idHopDong:int}")]
    [Authorize(Roles = "KHACH_HANG")]
    public async Task<IActionResult> GetMyByContract(int idHopDong)
    {
        try
        {
            var result = await _extensionService.GetMyByContractIdAsync(
                idHopDong, GetCurrentUserId());
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }


}