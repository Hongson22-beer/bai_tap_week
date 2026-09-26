using BtlThueXe.Core.DTOs.Customers;
namespace BtlThueXe.Core.Interfaces;
public interface ICustomerService
{
    Task<CustomerProfileResponseDto?> GetProfileByUserIdAsync(int userId);
    Task<List<CustomerProfileResponseDto>> GetAllCustomersAsync();
    Task<CustomerProfileResponseDto> UpdateProfileAsync(int userId, UpdateCustomerProfileRequestDto dto);
    Task<bool> VerifyCccdAsync(int customerId, bool daXacMinh);
    Task<bool> VerifyDocumentsAsync(int customerId, bool cccd, bool gplx);
    Task<string> SaveDocumentAsync(int userId, string documentType, Stream stream, string extension);
    Task<(byte[] Bytes, string ContentType)?> GetDocumentAsync(int userId, string documentType, bool staffCanRead);
}
