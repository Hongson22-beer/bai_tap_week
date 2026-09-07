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

    // API-Vehicle-List: Lấy toàn bộ danh sách xe (Public)
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var vehicles = await _vehicleService.GetAllAsync();
        return Ok(vehicles);
    }

    // Lấy chi tiết 1 xe theo ID
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var vehicle = await _vehicleService.GetByIdAsync(id);
        if (vehicle == null)
            return NotFound(new { message = "Không tìm thấy xe." });

        return Ok(vehicle);
    }

    // API-Vehicle-Available: Lọc xe còn trống theo khoảng thời gian nhận/trả
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

    // Thêm xe mới (Chỉ ADMIN hoặc NHAN_VIEN được thêm - kiểm tra Authorize)
    [HttpPost]
    [Authorize(Roles = "ADMIN,NHAN_VIEN")]
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
}