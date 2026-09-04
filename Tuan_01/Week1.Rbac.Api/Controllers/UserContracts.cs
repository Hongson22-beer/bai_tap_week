using System.ComponentModel.DataAnnotations;

namespace Week1.Rbac.Api.Contracts;

public sealed record CreateUserRequest(
    [property: Required, EmailAddress] string Email,
    [property: Required, MinLength(2)] string DisplayName,
    [property: Required, MinLength(8)] string Password
);

public sealed record UpdateUserRequest(
    [property: Required, MinLength(2)] string DisplayName,
    bool IsActive
);

public sealed record UserResponse(
    Guid Id,
    string Email,
    string DisplayName,
    bool IsActive,
    DateTimeOffset CreatedAt,
    IReadOnlyCollection<string> Roles
);