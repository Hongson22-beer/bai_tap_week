using System.Security.Claims;
using BtlThueXe.Core.DTOs.Customers;
using BtlThueXe.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BtlThueXe.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    // UC-05: Khách hàng xem hồ sơ cá nhân của chính mình
    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!int.TryParse(userIdStr, out int userId))
            return Unauthorized(new { message = "Không xác định được danh tính người dùng." });

        var profile = await _customerService.GetProfileByUserIdAsync(userId);
        if (profile == null)
            return NotFound(new { message = "Hồ sơ khách hàng chưa được khởi tạo." });

        return Ok(profile);
    }

    // Nhân viên / Quản trị viên xem danh sách khách hàng để quản lý
    [HttpGet]
    [Authorize(Roles = "ADMIN,NHAN_VIEN")]
    public async Task<IActionResult> GetAll()
    {
        var customers = await _customerService.GetAllCustomersAsync();
        return Ok(customers);
    }

    // UC-09, FR-05, BR-11: Nhân viên duyệt xác minh CCCD
    [HttpPatch("{id}/verify-cccd")]
    [Authorize(Roles = "ADMIN,NHAN_VIEN")]
    public async Task<IActionResult> VerifyCccd(int id, [FromBody] VerifyCccdRequestDto dto)
    {
        var result = await _customerService.VerifyCccdAsync(id, dto.DaXacMinh);
        if (!result)
            return NotFound(new { message = "Không tìm thấy khách hàng." });

        return Ok(new { message = "Cập nhật trạng thái xác minh CCCD thành công.", cccdDaXacMinh = dto.DaXacMinh });
    }
}