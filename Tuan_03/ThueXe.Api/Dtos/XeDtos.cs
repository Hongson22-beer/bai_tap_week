using System.ComponentModel.DataAnnotations;

namespace ThueXe.Api.Dtos;

public class XeCreateDto
{
    [Required]
    public int IdHangXe { get; set; }

    [Required]
    public int IdLoaiXe { get; set; }

    [Required, MaxLength(20)]
    public string BienSoXe { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? MauXe { get; set; }

    [Range(1990, 2100)]
    public int? NamSanXuat { get; set; }

    [Range(0, 100000000)]
    public decimal DonGiaNgay { get; set; }

    [RegularExpression("^(AVAILABLE|RESERVED|RENTING|MAINTENANCE|INACTIVE)$")]
    public string TrangThai { get; set; } = "AVAILABLE";

    public string? MoTa { get; set; }
}

public sealed class XeUpdateDto : XeCreateDto { }

public sealed record XeResponseDto(
    int Id,
    int IdHangXe,
    string TenHangXe,
    int IdLoaiXe,
    string TenLoaiXe,
    string BienSoXe,
    string? MauXe,
    int? NamSanXuat,
    decimal DonGiaNgay,
    string TrangThai,
    string? MoTa
);