using BtlThueXe.Core.DTOs.Vehicles;

namespace BtlThueXe.Core.Interfaces;

public interface IVehicleService
{
    Task<List<VehicleResponseDto>> GetAllAsync();
    Task<VehicleResponseDto?> GetByIdAsync(int id);
    Task<List<VehicleResponseDto>> GetAvailableVehiclesAsync(CheckAvailableQueryDto query);
    Task<VehicleResponseDto> CreateAsync(CreateVehicleDto dto);
    Task<VehicleResponseDto> UpdateAsync(int id, UpdateVehicleDto dto);
    Task<VehicleResponseDto> ChangeStatusAsync(int id, UpdateVehicleStatusDto dto, int idNguoiDung);
    Task<List<VehicleStatusHistoryDto>> GetStatusHistoryAsync(int id);
}