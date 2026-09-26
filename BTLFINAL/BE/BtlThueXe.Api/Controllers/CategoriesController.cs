using BtlThueXe.Core.DTOs.Vehicles;
using BtlThueXe.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BtlThueXe.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    // 1. Lấy danh sách Hãng xe (Public)
    [HttpGet("brands")]
    public async Task<IActionResult> GetBrands()
    {
        var brands = await _categoryService.GetBrandsAsync();
        return Ok(brands);
    }

    // 2. Thêm Hãng xe mới (Chỉ ADMIN)
    [HttpPost("brands")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> CreateBrand([FromBody] CreateBrandDto dto)
    {
        try
        {
            var brand = await _categoryService.CreateBrandAsync(dto);
            return StatusCode(201, brand);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // 3. Lấy danh sách Loại xe (Public)
    [HttpGet("types")]
    public async Task<IActionResult> GetVehicleTypes()
    {
        var types = await _categoryService.GetVehicleTypesAsync();
        return Ok(types);
    }

    // 4. Thêm Loại xe mới (Chỉ ADMIN)
    [HttpPost("types")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> CreateVehicleType([FromBody] CreateVehicleTypeDto dto)
    {
        try
        {
            var type = await _categoryService.CreateVehicleTypeAsync(dto);
            return StatusCode(201, type);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}