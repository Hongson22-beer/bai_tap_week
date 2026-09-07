using BtlThueXe.Core.DTOs.Evaluations;
using BtlThueXe.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BtlThueXe.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EvaluationsController : ControllerBase
{
    private readonly IEvaluationService _evaluationService;

    public EvaluationsController(
        IEvaluationService evaluationService)
    {
        _evaluationService = evaluationService;
    }

    // POST /api/Evaluations
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateEvaluationRequest request)
    {
        try
        {
            var result =
                await _evaluationService.CreateAsync(request);

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

    // GET /api/Evaluations
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result =
            await _evaluationService.GetAllAsync();

        return Ok(result);
    }

    // GET /api/Evaluations/1
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result =
            await _evaluationService.GetByIdAsync(id);

        if (result == null)
        {
            return NotFound(new
            {
                message = "Không tìm thấy đánh giá."
            });
        }

        return Ok(result);
    }

    // GET /api/Evaluations/contract/1
    [HttpGet("contract/{idHopDong:int}")]
    public async Task<IActionResult> GetByContract(
        int idHopDong)
    {
        var result =
            await _evaluationService
                .GetByContractIdAsync(idHopDong);

        if (result == null)
        {
            return NotFound(new
            {
                message =
                    "Hợp đồng chưa có đánh giá."
            });
        }

        return Ok(result);
    }
}