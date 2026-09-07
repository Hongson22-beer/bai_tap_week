using BtlThueXe.Core.DTOs.AuditLogs;
using BtlThueXe.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BtlThueXe.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogsController(
        IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateAuditLogRequest request)
    {
        try
        {
            var result =
                await _auditLogService.CreateAsync(request);

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.Id },
                result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result =
            await _auditLogService.GetAllAsync();

        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        var result =
            await _auditLogService.GetByIdAsync(id);

        if (result == null)
        {
            return NotFound(new
            {
                message = "Không tìm thấy audit log."
            });
        }

        return Ok(result);
    }

    [HttpGet("user/{idNguoiDung:int}")]
    public async Task<IActionResult> GetByUser(
        int idNguoiDung)
    {
        var result =
            await _auditLogService
                .GetByUserIdAsync(idNguoiDung);

        return Ok(result);
    }
}