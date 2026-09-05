using Microsoft.AspNetCore.Mvc;
using ThueXe.Api.Dtos;
using ThueXe.Api.Services;

namespace ThueXe.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LoaiXeController : ControllerBase
{
    private readonly ILoaiXeService _service;

    public LoaiXeController(ILoaiXeService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _service.GetAllAsync();
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateLoaiXeDto dto)
    {
        try
        {
            var result = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateLoaiXeDto dto)
    {
        try
        {
            var updated = await _service.UpdateAsync(id, dto);
            if (!updated) return NotFound();
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var (success, errorMessage) = await _service.DeleteAsync(id);

        if (!success)
        {
            if (errorMessage == "NotFound") return NotFound();
            return StatusCode(StatusCodes.Status409Conflict, new { message = errorMessage });
        }

        return NoContent();
    }
}