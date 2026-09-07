using BtlThueXe.Core.DTOs.Auth;

namespace BtlThueXe.Core.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterCustomerAsync(RegisterRequestDto dto);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto dto);
}