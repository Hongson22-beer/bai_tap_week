using BtlThueXe.Core.DTOs.Vehicles;

namespace BtlThueXe.Core.Interfaces;

public interface ICategoryService
{
    Task<List<BrandDto>> GetBrandsAsync();
    Task<BrandDto> CreateBrandAsync(CreateBrandDto dto);
    Task<List<VehicleTypeDto>> GetVehicleTypesAsync();
    Task<VehicleTypeDto> CreateVehicleTypeAsync(CreateVehicleTypeDto dto);
}