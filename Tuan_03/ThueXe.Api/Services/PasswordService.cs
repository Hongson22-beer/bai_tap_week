using Microsoft.AspNetCore.Identity;
using ThueXe.Api.Models;

namespace ThueXe.Api.Services;

public interface IPasswordService
{
    string HashPassword(AppUser user, string password);
    bool VerifyPassword(AppUser user, string hashedPassword, string providedPassword);
}

public class PasswordService : IPasswordService
{
    private readonly PasswordHasher<AppUser> _hasher = new();

    public string HashPassword(AppUser user, string password)
    {
        return _hasher.HashPassword(user, password);
    }

    public bool VerifyPassword(AppUser user, string hashedPassword, string providedPassword)
    {
        var result = _hasher.VerifyHashedPassword(user, hashedPassword, providedPassword);
        return result != PasswordVerificationResult.Failed;
    }
}