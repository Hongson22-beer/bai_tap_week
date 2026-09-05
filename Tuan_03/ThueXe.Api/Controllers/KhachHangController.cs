using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThueXe.Api.Dtos;

namespace ThueXe.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class KhachHangController : ControllerBase
{
    private readonly IAuthorizationService _authorizationService;

    public KhachHangController(IAuthorizationService authorizationService)
    {
        _authorizationService = authorizationService;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var authResult = await _authorizationService.AuthorizeAsync(User, id, "CanManageCustomerProfile");
        
        if (!authResult.Succeeded)
        {
            return Forbid(); // Trả về 403 Forbidden nếu không phải chính chủ hoặc Admin/Staff
        }

        return Ok(new { Id = id, HoTen = "Khách hàng mẫu", SoDienThoai = "0987654321" });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateProfile(Guid id, [FromBody] UpdateCustomerRequest request)
    {
        var authResult = await _authorizationService.AuthorizeAsync(User, id, "CanManageCustomerProfile");

        if (!authResult.Succeeded)
        {
            return Forbid(); // Trả về 403 Forbidden nếu không phải chính chủ
        }

        return Ok(new { Message = $"Cập nhật hồ sơ khách hàng {id} thành công." });
    }
}