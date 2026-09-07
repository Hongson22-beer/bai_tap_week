using System.ComponentModel.DataAnnotations;

namespace ThueXe.Api.Dtos;
public record RegisterRequestDto(string Email, string Password, string FullName);
public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password
);

public record AuthResponse(
    string AccessToken,
    string Email,
    string FullName,
    List<string> Roles
);