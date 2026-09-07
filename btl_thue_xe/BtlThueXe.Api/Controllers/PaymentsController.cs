using BtlThueXe.Core.DTOs.Payments;
using BtlThueXe.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BtlThueXe.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreatePaymentRequest request)
    {
        try
        {
            var result = await _paymentService.CreateAsync(request);

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.Id },
                result
            );
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
        var result = await _paymentService.GetByIdAsync(id);

        if (result == null)
            return NotFound(new
            {
                message = "Không tìm thấy thanh toán."
            });

        return Ok(result);
    }

    [HttpGet("contract/{idHopDong:int}")]
    public async Task<IActionResult> GetByContract(int idHopDong)
    {
        var result =
            await _paymentService.GetByContractIdAsync(idHopDong);

        return Ok(result);
    }
}