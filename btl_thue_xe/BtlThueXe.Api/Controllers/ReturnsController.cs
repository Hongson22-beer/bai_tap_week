using BtlThueXe.Core.DTOs.Returns;
using BtlThueXe.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BtlThueXe.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReturnsController : ControllerBase
{
    private readonly IReturnService _returnService;

    public ReturnsController(
        IReturnService returnService)
    {
        _returnService = returnService;
    }

    // POST /api/Returns
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateReturnRequest request)
    {
        try
        {
            var result =
                await _returnService.CreateAsync(request);

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

    // GET /api/Returns/1
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result =
            await _returnService.GetByIdAsync(id);

        if (result == null)
        {
            return NotFound(new
            {
                message =
                    "Không tìm thấy thông tin trả xe."
            });
        }

        return Ok(result);
    }

    // GET /api/Returns/contract/1
    [HttpGet("contract/{idHopDong:int}")]
    public async Task<IActionResult>
        GetByContract(int idHopDong)
    {
        var result =
            await _returnService
                .GetByContractIdAsync(idHopDong);

        if (result == null)
        {
            return NotFound(new
            {
                message =
                    "Hợp đồng chưa có thông tin trả xe."
            });
        }

        return Ok(result);
    }
}