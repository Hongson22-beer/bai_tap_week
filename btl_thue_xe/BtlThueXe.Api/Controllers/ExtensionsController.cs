using BtlThueXe.Core.DTOs.Extensions;
using BtlThueXe.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BtlThueXe.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExtensionsController : ControllerBase
{
    private readonly IExtensionService _extensionService;

    public ExtensionsController(
        IExtensionService extensionService)
    {
        _extensionService = extensionService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateExtensionRequest request)
    {
        try
        {
            var result =
                await _extensionService.CreateAsync(request);

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
            await _extensionService.GetByIdAsync(id);

        if (result == null)
        {
            return NotFound(new
            {
                message =
                    "Không tìm thấy yêu cầu gia hạn."
            });
        }

        return Ok(result);
    }

    [HttpGet("contract/{idHopDong:int}")]
    public async Task<IActionResult>
        GetByContract(int idHopDong)
    {
        var result =
            await _extensionService
                .GetByContractIdAsync(idHopDong);

        return Ok(result);
    }
    [HttpPut("{id:int}/process")]
public async Task<IActionResult> Process(
    int id,
    [FromBody] ProcessExtensionRequest request)
{
    try
    {
        var result =
            await _extensionService.ProcessAsync(
                id,
                request);

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