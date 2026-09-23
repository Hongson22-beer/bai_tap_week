using System.Security.Claims;
using BtlThueXe.Core.DTOs.Handovers;
using BtlThueXe.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BtlThueXe.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HandoversController : ControllerBase
{
    private readonly IHandOverService _handOverService;

    public HandoversController(IHandOverService handOverService)
    {
        _handOverService = handOverService;
    }

    // =========================================================
    // LẤY ID NGƯỜI DÙNG TỪ JWT
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
    // POST /api/handovers
    // NHÂN VIÊN BÀN GIAO XE
    // =========================================================
    [HttpPost]
    [Authorize(Roles = "NHAN_VIEN")]
    public async Task<IActionResult> Create(
        [FromBody] CreateHandoverRequest request)
    {
        try
        {
            int currentUserId = GetCurrentUserId();

            // IdNhanVien trong request không được tin cậy.
            // Service sẽ sử dụng currentUserId lấy từ JWT.
            var result = await _handOverService.CreateAsync(
                request,
                currentUserId);

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.Id },
                result);
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
    // GET /api/handovers/{id}
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
                message = "ID bàn giao không hợp lệ."
            });
        }

        var result =
            await _handOverService.GetByIdAsync(id);

        if (result == null)
        {
            return NotFound(new
            {
                message = "Không tìm thấy thông tin bàn giao xe."
            });
        }

        return Ok(result);
    }

    // =========================================================
    // GET /api/handovers/contract/{idHopDong}
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
                message = "ID hợp đồng không hợp lệ."
            });
        }

        var result =
            await _handOverService.GetByContractIdAsync(
                idHopDong);

        if (result == null)
        {
            return NotFound(new
            {
                message =
                    "Không tìm thấy thông tin bàn giao của hợp đồng."
            });
        }

        return Ok(result);
    }
}