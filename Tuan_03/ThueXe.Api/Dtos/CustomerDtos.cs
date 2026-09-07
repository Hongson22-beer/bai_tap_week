namespace ThueXe.Api.Dtos;

public sealed record CustomerResponse(
    Guid Id,
    string HoTen,
    string SoDienThoai,
    string DiaChi
);

public sealed record UpdateCustomerRequest(
    string? HoTen,
    string? SoDienThoai,
    string? DiaChi
);