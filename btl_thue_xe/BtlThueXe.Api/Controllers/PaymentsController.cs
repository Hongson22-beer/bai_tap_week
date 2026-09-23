using System.Security.Claims;
using BtlThueXe.Core.DTOs.Payments;
using BtlThueXe.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BtlThueXe.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IConfiguration _configuration;

    public PaymentsController(
        IPaymentService paymentService,
        IConfiguration configuration)
    {
        _paymentService = paymentService;
        _configuration = configuration;
    }

    // =====================================================
    // LẤY USER ID TỪ JWT
    // =====================================================
    private int GetCurrentUserId()
    {
        var userIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? User.FindFirstValue("id");

        if (string.IsNullOrWhiteSpace(userIdValue) ||
            !int.TryParse(userIdValue, out var userId))
        {
            throw new UnauthorizedAccessException(
                "Không xác định được người dùng từ JWT.");
        }

        return userId;
    }

    // =====================================================
    // LẤY ROLE TỪ JWT
    // =====================================================
    private string? GetCurrentRole()
    {
        return User.FindFirstValue(ClaimTypes.Role);
    }

    // =====================================================
    // TẠO THANH TOÁN
    // CUSTOMER / NHAN_VIEN
    // POST /api/Payments
    // =====================================================
    [HttpPost]
    [Authorize(Roles = "KHACH_HANG,NHAN_VIEN")]
    public async Task<IActionResult> Create(
        [FromBody] CreatePaymentRequest request)
    {
        try
        {
            int currentUserId =
                GetCurrentUserId();

            string? currentUserRole =
                GetCurrentRole();

            var result =
                await _paymentService.CreateAsync(
                    request,
                    currentUserId,
                    currentUserRole);

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.Id },
                result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new
                {
                    message = ex.Message
                });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new
            {
                message = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new
            {
                message = ex.Message
            });
        }
    }

    // =====================================================
    // WEBHOOK THANH TOÁN CHUYỂN KHOẢN
    // POST /api/Payments/webhook
    // =====================================================
    [AllowAnonymous]
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook(
        [FromHeader(Name = "X-Webhook-Secret")]
        string? webhookSecret,
        [FromBody] PaymentWebhookRequest request)
    {
        string? configuredSecret =
            _configuration["Payment:WebhookSecret"];

        if (string.IsNullOrWhiteSpace(configuredSecret))
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new
                {
                    message =
                        "Payment WebhookSecret chưa được cấu hình."
                });
        }

        if (string.IsNullOrWhiteSpace(webhookSecret) ||
            webhookSecret != configuredSecret)
        {
            return Unauthorized(new
            {
                message =
                    "Webhook secret không hợp lệ."
            });
        }

        try
        {
            var result =
                await _paymentService
                    .ProcessWebhookAsync(request);

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new
            {
                message = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new
            {
                message = ex.Message
            });
        }
    }

    // =====================================================
    // XEM THANH TOÁN THEO ID
    // NHAN_VIEN / ADMIN
    // GET /api/Payments/1
    // =====================================================
    [HttpGet("{id:int}")]
    [Authorize(Roles = "NHAN_VIEN,ADMIN")]
    public async Task<IActionResult> GetById(int id)
    {
        var result =
            await _paymentService.GetByIdAsync(id);

        if (result == null)
        {
            return NotFound(new
            {
                message =
                    "Không tìm thấy thanh toán."
            });
        }

        return Ok(result);
    }

    // =====================================================
    // XEM THANH TOÁN THEO HỢP ĐỒNG
    // NHAN_VIEN / ADMIN
    // GET /api/Payments/contract/1
    // =====================================================
    [HttpGet("contract/{idHopDong:int}")]
    [Authorize(Roles = "NHAN_VIEN,ADMIN")]
    public async Task<IActionResult> GetByContract(
        int idHopDong)
    {
        if (idHopDong <= 0)
        {
            return BadRequest(new
            {
                message =
                    "ID hợp đồng không hợp lệ."
            });
        }

        var result =
            await _paymentService
                .GetByContractIdAsync(idHopDong);

        return Ok(result);
    }
}