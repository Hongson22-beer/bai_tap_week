using System.ComponentModel.DataAnnotations;

namespace ThueXe.Api.Dtos
{
    public class HangXeReadDto
    {
        public int Id { get; set; }
        public string TenHang { get; set; } = string.Empty;
        public string? QuocGia { get; set; }
        public string? MoTa { get; set; }
    }

    public class CreateHangXeDto
    {
        [Required(ErrorMessage = "Tên hãng xe không được để trống")]
        [StringLength(100, ErrorMessage = "Tên hãng xe không quá 100 ký tự")]
        public string TenHang { get; set; } = string.Empty;

        [StringLength(100)]
        public string? QuocGia { get; set; }

        [StringLength(500)]
        public string? MoTa { get; set; }
    }

    public class UpdateHangXeDto : CreateHangXeDto
    {
    }
}