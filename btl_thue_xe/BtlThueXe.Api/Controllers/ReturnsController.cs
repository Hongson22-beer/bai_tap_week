using System.Security.Claims;
using BtlThueXe.Core.DTOs.Returns;
using BtlThueXe.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BtlThueXe.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReturnsController : ControllerBase
{
    private readonly IReturnService _returnService;

    public ReturnsController(IReturnService returnService)
    {
        _returnService = returnService;
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
    // POST /api/returns
    // NHÂN VIÊN TIẾP NHẬN XE TRẢ
    // =========================================================
    [HttpPost]
    [Authorize(Roles = "NHAN_VIEN")]
    public async Task<IActionResult> Create(
        [FromBody] CreateReturnRequest request)
    {
        try
        {
            int currentUserId = GetCurrentUserId();

            // IdNhanVien trong request không được tin cậy.
            // Service sử dụng ID nhân viên lấy từ JWT.
            var result = await _returnService.CreateAsync(
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
    // GET /api/returns/{id}
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
                message = "ID trả xe không hợp lệ."
            });
        }

        var result =
            await _returnService.GetByIdAsync(id);

        if (result == null)
        {
            return NotFound(new
            {
                message = "Không tìm thấy thông tin trả xe."
            });
        }

        return Ok(result);
    }

    // =========================================================
    // GET /api/returns/contract/{idHopDong}
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
            await _returnService.GetByContractIdAsync(
                idHopDong);

        if (result == null)
        {
            return NotFound(new
            {
                message =
                    "Không tìm thấy thông tin trả xe của hợp đồng."
            });
        }

        return Ok(result);
    }
}