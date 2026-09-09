using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using BtlThueXe.Core.DTOs.Contracts;
using BtlThueXe.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BtlThueXe.Api.Controllers
{
    [ApiController]
    [Route("api/contracts")]
    [Authorize]
    public class ContractsController : ControllerBase
    {
        private readonly IContractService _contractService;

        public ContractsController(IContractService contractService)
        {
            _contractService = contractService;
        }

        #region Helper Methods

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                           ?? User.FindFirst("sub")
                           ?? User.FindFirst("id");

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                throw new UnauthorizedAccessException("Không thể xác định thông tin người dùng từ token.");
            }

            return userId;
        }

        private string? GetCurrentUserRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value
                ?? User.FindFirst("role")?.Value;
        }

        #endregion

        #region 1. POST /api/contracts

        [HttpPost]
        [Authorize(Roles = "NHAN_VIEN")]
        public async Task<IActionResult> CreateContract([FromBody] CreateContractRequestDto request)
        {
            int staffUserId = GetCurrentUserId();
            string? currentUserRole = GetCurrentUserRole();

            var result = await _contractService.CreateContractAsync(request, staffUserId, currentUserRole);

            return CreatedAtAction(
                nameof(GetContractById),
                new { id = result.Id },
                result);
        }

        #endregion

        #region 2. GET /api/contracts/{id}

        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<ActionResult<ContractDetailDto>> GetContractById(int id)
        {
            int currentUserId = GetCurrentUserId();
            string? currentUserRole = GetCurrentUserRole();

            var result = await _contractService.GetContractByIdAsync(id, currentUserId, currentUserRole);

            if (result == null)
            {
                return NotFound(new { message = $"Không tìm thấy hợp đồng có ID = {id}." });
            }

            return Ok(result);
        }

        #endregion

        #region 3. PUT /api/contracts/{id}/send

        [HttpPut("{id:int}/send")]
        [Authorize(Roles = "NHAN_VIEN")]
        public async Task<ActionResult<ContractResponseDto>> SendContract(int id)
        {
            int staffUserId = GetCurrentUserId();
            string? currentUserRole = GetCurrentUserRole();

            var result = await _contractService.SendContractAsync(id, staffUserId, currentUserRole);

            return Ok(result);
        }

        #endregion

        #region 4. PUT /api/contracts/{id}/confirm

        [HttpPut("{id:int}/confirm")]
        [Authorize(Roles = "KHACH_HANG")]
        public async Task<ActionResult<ContractResponseDto>> ConfirmContract(int id)
        {
            int currentUserId = GetCurrentUserId();
            string? currentUserRole = GetCurrentUserRole();

            var result = await _contractService.ConfirmContractAsync(id, currentUserId, currentUserRole);

            return Ok(result);
        }

        #endregion

        #region 5. PUT /api/contracts/{id}/reject

        [HttpPut("{id:int}/reject")]
        [Authorize(Roles = "KHACH_HANG")]
        public async Task<ActionResult<ContractResponseDto>> RejectContract(
            int id,
            [FromBody] RejectContractRequestDto request)
        {
            int currentUserId = GetCurrentUserId();
            string? currentUserRole = GetCurrentUserRole();

            var result = await _contractService.RejectContractAsync(id, request, currentUserId, currentUserRole);

            return Ok(result);
        }

        #endregion

        #region 6. GET /api/contracts/{id}/history

        [HttpGet("{id:int}/history")]
        [Authorize]
        public async Task<ActionResult<List<ContractHistoryDto>>> GetContractHistory(int id)
        {
            int currentUserId = GetCurrentUserId();
            string? currentUserRole = GetCurrentUserRole();

            var result = await _contractService.GetContractHistoryAsync(id, currentUserId, currentUserRole);

            return Ok(result);
        }

        #endregion
    }
}