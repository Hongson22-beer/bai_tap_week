using System.Security.Claims;
using BtlThueXe.Core.DTOs.Vehicles;
using BtlThueXe.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BtlThueXe.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VehiclesController : ControllerBase
{
    private readonly IVehicleService _vehicleService;

    public VehiclesController(IVehicleService vehicleService)
    {
        _vehicleService = vehicleService;
    }

    // 1. Lấy toàn bộ danh sách xe (Public - Khách hàng, Nhân viên, Admin đều xem được)
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var vehicles = await _vehicleService.GetAllAsync();
        return Ok(vehicles);
    }

    // 2. Lấy chi tiết 1 xe theo ID (Public)
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var vehicle = await _vehicleService.GetByIdAsync(id);
        if (vehicle == null)
            return NotFound(new { message = "Không tìm thấy xe." });

        return Ok(vehicle);
    }

    // 3. Lọc xe còn trống theo khoảng thời gian nhận/trả (Public - UC-03, FR-04)
    [HttpGet("available")]
    public async Task<IActionResult> GetAvailable([FromQuery] CheckAvailableQueryDto query)
    {
        try
        {
            var vehicles = await _vehicleService.GetAvailableVehiclesAsync(query);
            return Ok(vehicles);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // 4. Thêm xe mới vào hệ thống (Chỉ ADMIN - UC-04, FR-07)
    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Create([FromBody] CreateVehicleDto dto)
    {
        try
        {
            var vehicle = await _vehicleService.CreateAsync(dto);
            return StatusCode(201, vehicle);
        }
        catch (Exception ex)
        {
            var msg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
            return BadRequest(new { message = msg });
        }
    }

    // 5. Sửa thông tin định danh, đơn giá thuê xe (Chỉ ADMIN - UC-04, FR-07)
    [HttpPut("{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateVehicleDto dto)
    {
        try
        {
            var vehicle = await _vehicleService.UpdateAsync(id, dto);
            return Ok(vehicle);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // 6. Đổi trạng thái xe sang Bảo dưỡng/Sẵn sàng (ADMIN và NHAN_VIEN vận hành - UC-04, FR-07)
    [HttpPatch("{id}/status")]
    [Authorize(Roles = "ADMIN,NHAN_VIEN")]
    public async Task<IActionResult> ChangeStatus(int id, [FromBody] UpdateVehicleStatusDto dto)
    {
        try
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            int userId = int.TryParse(userIdStr, out var parsed) ? parsed : 1;

            var vehicle = await _vehicleService.ChangeStatusAsync(id, dto, userId);
            return Ok(vehicle);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // 7. Xem lịch sử thay đổi trạng thái xe (ADMIN và NHAN_VIEN - UC-23, FR-23)
    [HttpGet("{id}/history")]
    [Authorize(Roles = "ADMIN,NHAN_VIEN")]
    public async Task<IActionResult> GetHistory(int id)
    {
        var history = await _vehicleService.GetStatusHistoryAsync(id);
        return Ok(history);
    }
}