using BtlThueXe.Core.DTOs.Vehicles;

namespace BtlThueXe.Core.Interfaces;

public interface IVehicleService
{
    Task<List<VehicleResponseDto>> GetAllAsync();
    Task<VehicleResponseDto?> GetByIdAsync(int id);
    Task<List<VehicleResponseDto>> GetAvailableVehiclesAsync(CheckAvailableQueryDto query);
    Task<VehicleResponseDto> CreateAsync(CreateVehicleDto dto);
}