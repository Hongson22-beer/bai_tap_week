using BtlThueXe.Core.DTOs.Cancellations;
using BtlThueXe.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BtlThueXe.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CancellationsController : ControllerBase
{
    private readonly ICancellationService _cancellationService;

    public CancellationsController(
        ICancellationService cancellationService)
    {
        _cancellationService = cancellationService;
    }

    // POST /api/Cancellations
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateCancellationRequest request)
    {
        try
        {
            var result =
                await _cancellationService.CreateAsync(request);

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

    // GET /api/Cancellations/1
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result =
            await _cancellationService.GetByIdAsync(id);

        if (result == null)
        {
            return NotFound(new
            {
                message =
                    "Không tìm thấy yêu cầu hủy hợp đồng."
            });
        }

        return Ok(result);
    }

    // GET /api/Cancellations/contract/1
    [HttpGet("contract/{idHopDong:int}")]
    public async Task<IActionResult> GetByContract(
        int idHopDong)
    {
        var result =
            await _cancellationService
                .GetByContractIdAsync(idHopDong);

        return Ok(result);
    }

    // PUT /api/Cancellations/1/process
    [HttpPut("{id:int}/process")]
    public async Task<IActionResult> Process(
        int id,
        [FromBody] ProcessCancellationRequest request)
    {
        try
        {
            var result =
                await _cancellationService
                    .ProcessAsync(id, request);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }
}