using System.Security.Claims;
using BtlThueXe.Core.DTOs.Evaluations;
using BtlThueXe.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BtlThueXe.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EvaluationsController : ControllerBase
{
    private readonly IEvaluationService _evaluationService;

    public EvaluationsController(
        IEvaluationService evaluationService)
    {
        _evaluationService = evaluationService;
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
    // POST /api/evaluations
    // KHÁCH HÀNG TẠO ĐÁNH GIÁ
    // =========================================================
    [HttpPost]
    [Authorize(Roles = "KHACH_HANG")]
    public async Task<IActionResult> Create(
        [FromBody] CreateEvaluationRequest request)
    {
        try
        {
            int currentUserId =
                GetCurrentUserId();

            // Không tin IdKhachHang từ body.
            // Service sẽ:
            // NguoiDung.Id từ JWT
            //      ↓
            // KhachHang.IdNguoiDung
            //      ↓
            // KhachHang.Id
            var result =
                await _evaluationService.CreateAsync(
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
    // GET /api/evaluations
    // NHÂN VIÊN / ADMIN
    // =========================================================
    [HttpGet]
    [Authorize(Roles = "NHAN_VIEN,ADMIN")]
    public async Task<IActionResult> GetAll()
    {
        var result =
            await _evaluationService.GetAllAsync();

        return Ok(result);
    }

    // =========================================================
    // GET /api/evaluations/{id}
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
                    "ID đánh giá không hợp lệ."
            });
        }

        var result =
            await _evaluationService.GetByIdAsync(id);

        if (result == null)
        {
            return NotFound(new
            {
                message =
                    "Không tìm thấy đánh giá."
            });
        }

        return Ok(result);
    }

    // =========================================================
    // GET /api/evaluations/contract/{idHopDong}
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
            await _evaluationService
                .GetByContractIdAsync(idHopDong);

        if (result == null)
        {
            return NotFound(new
            {
                message =
                    "Hợp đồng chưa có đánh giá."
            });
        }

        return Ok(result);
    }
}