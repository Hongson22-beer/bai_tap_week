using System.ComponentModel.DataAnnotations;

namespace ThueXe.Api.Dtos;

public record CreateLoaiXeDto(
    [Required] string MaLoai,
    [Required] string TenLoai,
    [Range(1, int.MaxValue, ErrorMessage = "Số chỗ ngồi phải lớn hơn 0")] int SoCho,
    string? MoTa
);

public record UpdateLoaiXeDto(
    [Required] string TenLoai,
    [Range(1, int.MaxValue, ErrorMessage = "Số chỗ ngồi phải lớn hơn 0")] int SoCho,
    string? MoTa
);

public record LoaiXeResponseDto(
    int Id,
    string MaLoai,
    string TenLoai,
    int SoCho,
    string? MoTa,
    int SoLuongXe
);