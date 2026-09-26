using BtlThueXe.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BtlThueXe.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "ADMIN")]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogsController(
        IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    // =====================================================
    // ADMIN XEM TOÀN BỘ AUDIT LOG
    // GET /api/AuditLogs
    // =====================================================
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result =
            await _auditLogService.GetAllAsync();

        return Ok(result);
    }

    // =====================================================
    // ADMIN XEM AUDIT LOG THEO ID
    // GET /api/AuditLogs/1
    // =====================================================
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        var result =
            await _auditLogService.GetByIdAsync(id);

        if (result == null)
        {
            return NotFound(new
            {
                message =
                    "Không tìm thấy audit log."
            });
        }

        return Ok(result);
    }

    // =====================================================
    // ADMIN XEM AUDIT LOG THEO NGƯỜI DÙNG
    // GET /api/AuditLogs/user/1
    // =====================================================
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