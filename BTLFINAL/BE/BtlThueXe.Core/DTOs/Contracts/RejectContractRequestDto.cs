using System.ComponentModel.DataAnnotations;

namespace BtlThueXe.Core.DTOs.Contracts
{
    public class RejectContractRequestDto
    {
        [Required(ErrorMessage = "Lý do từ chối không được để trống.")]
        [StringLength(500, ErrorMessage = "Lý do từ chối không vượt quá 500 ký tự.")]
        public string LyDo { get; set; } = string.Empty;
    }
}