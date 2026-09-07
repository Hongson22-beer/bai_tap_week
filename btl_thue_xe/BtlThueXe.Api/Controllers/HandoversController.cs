using BtlThueXe.Core.DTOs.Handovers;
using BtlThueXe.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BtlThueXe.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HandoversController : ControllerBase
{
    private readonly IHandOverService _handOverService;

    public HandoversController(
        IHandOverService handOverService)
    {
        _handOverService = handOverService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateHandoverRequest request)
    {
        try
        {
            var result =
                await _handOverService.CreateAsync(request);

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

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result =
            await _handOverService.GetByIdAsync(id);

        if (result == null)
        {
            return NotFound(new
            {
                message =
                    "Không tìm thấy thông tin bàn giao."
            });
        }

        return Ok(result);
    }

    [HttpGet("contract/{idHopDong:int}")]
    public async Task<IActionResult>
        GetByContract(int idHopDong)
    {
        var result =
            await _handOverService
                .GetByContractIdAsync(idHopDong);

        if (result == null)
        {
            return NotFound(new
            {
                message =
                    "Hợp đồng chưa có thông tin bàn giao."
            });
        }

        return Ok(result);
    }
}