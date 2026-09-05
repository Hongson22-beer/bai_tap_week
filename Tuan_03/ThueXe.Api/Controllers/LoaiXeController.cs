using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ThueXe.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LoaiXeController : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public IActionResult GetAll()
    {
        return Ok(new[] { new { Id = Guid.NewGuid(), TenLoai = "SUV 7 chỗ" } });
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    public IActionResult Create([FromBody] object dto)
    {
        return Ok(new { Message = "Thêm loại xe thành công" });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Staff")]
    public IActionResult Update(Guid id, [FromBody] object dto)
    {
        return Ok(new { Message = $"Cập nhật loại xe {id} thành công" });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Staff")]
    public IActionResult Delete(Guid id)
    {
        return Ok(new { Message = $"Xóa loại xe {id} thành công" });
    }
}