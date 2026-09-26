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
    public HandoversController(IHandOverService handOverService) => _handOverService = handOverService;

    private int GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub") ?? User.FindFirst("id");
        if (claim == null || !int.TryParse(claim.Value, out var id))
            throw new UnauthorizedAccessException("Không xác định được người dùng từ JWT.");
        return id;
    }

    [HttpPost]
    [Authorize(Roles = "NHAN_VIEN")]
    public async Task<IActionResult> Create([FromBody] CreateHandoverRequest request)
    {
        try
        {
            var result = await _handOverService.CreateAsync(request, GetCurrentUserId());
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
        var result = await _handOverService.GetByIdAsync(id);
        return result == null ? NotFound(new { message = "Không tìm thấy thông tin bàn giao xe." }) : Ok(result);
    }

    [HttpGet("contract/{idHopDong:int}")]
    [Authorize(Roles = "KHACH_HANG,NHAN_VIEN,ADMIN")]
    public async Task<IActionResult> GetByContract(int idHopDong)
    {
        try
        {
            var isStaff = User.IsInRole("NHAN_VIEN") || User.IsInRole("ADMIN");
            var result = await _handOverService.GetByContractForUserAsync(idHopDong, GetCurrentUserId(), isStaff);
            return result == null ? NotFound(new { message = "Không tìm thấy thông tin bàn giao của hợp đồng." }) : Ok(result);
        }
        catch (UnauthorizedAccessException ex) { return Forbid(); }
    }

    [HttpPost("contract/{idHopDong:int}/confirm-received")]
    [Authorize(Roles = "KHACH_HANG")]
    public async Task<IActionResult> ConfirmReceived(int idHopDong)
    {
        try
        {
            var result = await _handOverService.ConfirmReceivedAsync(idHopDong, GetCurrentUserId());
            return Ok(result);
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }
}
