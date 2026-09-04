using Microsoft.AspNetCore.Mvc;
using ThueXe.Api.Dtos;
using ThueXe.Api.Services;

namespace ThueXe.Api.Controllers;

[ApiController]
[Route("api/xe")]
public class XeController(IXeService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<XeResponseDto>>> GetAll(CancellationToken ct) =>
        Ok(await service.GetAllAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<XeResponseDto>> GetById(int id, CancellationToken ct)
    {
        var item = await service.GetByIdAsync(id, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<XeResponseDto>> Create(XeCreateDto dto, CancellationToken ct)
    {
        var item = await service.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<XeResponseDto>> Update(int id, XeUpdateDto dto, CancellationToken ct)
    {
        var item = await service.UpdateAsync(id, dto, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct) =>
        await service.DeleteAsync(id, ct) ? NoContent() : NotFound();
}