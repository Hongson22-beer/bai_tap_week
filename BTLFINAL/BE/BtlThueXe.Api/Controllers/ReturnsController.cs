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
    public ReturnsController(IReturnService returnService) => _returnService = returnService;

    private int GetCurrentUserId()
    {
        var c = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub") ?? User.FindFirst("id");
        if (c == null || !int.TryParse(c.Value, out var id))
            throw new UnauthorizedAccessException("Không xác định được người dùng từ JWT.");
        return id;
    }

    private bool IsStaffOrAdmin() => User.IsInRole("NHAN_VIEN") || User.IsInRole("ADMIN");

    [HttpPost("contract/{idHopDong:int}/request")]
    [Authorize(Roles = "KHACH_HANG")]
    public async Task<IActionResult> RequestReturn(int idHopDong, [FromBody] CreateReturnIntentRequest request)
    {
        try { return Ok(await _returnService.RequestReturnAsync(idHopDong, request, GetCurrentUserId())); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpGet("contract/{idHopDong:int}/request")]
    [Authorize(Roles = "KHACH_HANG,NHAN_VIEN,ADMIN")]
    public async Task<IActionResult> GetReturnRequest(int idHopDong)
    {
        try
        {
            var r = await _returnService.GetReturnRequestAsync(idHopDong, GetCurrentUserId(), IsStaffOrAdmin());
            return r == null ? NotFound(new { message = "Chưa có yêu cầu trả xe." }) : Ok(r);
        }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
    }

    [HttpPost]
    [Authorize(Roles = "NHAN_VIEN")]
    public async Task<IActionResult> Create([FromBody] CreateReturnRequest request)
    {
        try
        {
            var result = await _returnService.CreateAsync(request, GetCurrentUserId());
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(new { message = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = "NHAN_VIEN,ADMIN")]
    public async Task<IActionResult> GetById(int id)
    {
        if (id <= 0) return BadRequest(new { message = "ID trả xe không hợp lệ." });
        var result = await _returnService.GetByIdAsync(id);
        return result == null ? NotFound(new { message = "Không tìm thấy thông tin trả xe." }) : Ok(result);
    }

    [HttpGet("contract/{idHopDong:int}")]
    [Authorize(Roles = "KHACH_HANG,NHAN_VIEN,ADMIN")]
    public async Task<IActionResult> GetByContract(int idHopDong)
    {
        if (idHopDong <= 0) return BadRequest(new { message = "ID hợp đồng không hợp lệ." });
        try
        {
            if (!IsStaffOrAdmin())
                await _returnService.GetReturnRequestAsync(idHopDong, GetCurrentUserId(), false);
            var result = await _returnService.GetByContractIdAsync(idHopDong);
            return result == null ? NotFound(new { message = "Không tìm thấy thông tin trả xe của hợp đồng." }) : Ok(result);
        }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
    }
}
