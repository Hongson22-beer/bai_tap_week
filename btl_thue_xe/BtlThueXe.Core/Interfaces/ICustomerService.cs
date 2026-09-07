using BtlThueXe.Core.DTOs.Customers;

namespace BtlThueXe.Core.Interfaces;

public interface ICustomerService
{
    Task<CustomerProfileResponseDto?> GetProfileByUserIdAsync(int userId);
    Task<List<CustomerProfileResponseDto>> GetAllCustomersAsync();
    Task<bool> VerifyCccdAsync(int customerId, bool daXacMinh);
}