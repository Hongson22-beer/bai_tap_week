using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ThueXe.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class HangXeController : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public IActionResult GetAll()
    {
        return Ok(new[] { new { Id = Guid.NewGuid(), TenHang = "Toyota" } });
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    public IActionResult Create([FromBody] object dto)
    {
        return Ok(new { Message = "Thêm hãng xe thành công" });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Staff")]
    public IActionResult Update(Guid id, [FromBody] object dto)
    {
        return Ok(new { Message = $"Cập nhật hãng xe {id} thành công" });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Staff")]
    public IActionResult Delete(Guid id)
    {
        return Ok(new { Message = $"Xóa hãng xe {id} thành công" });
    }
}