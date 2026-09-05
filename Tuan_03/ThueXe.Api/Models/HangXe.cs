using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ThueXe.Api.Models
{
    [Table("HangXe")]
    public class HangXe
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string TenHang { get; set; } = string.Empty;

        [StringLength(100)]
        public string? QuocGia { get; set; }

        [StringLength(500)]
        public string? MoTa { get; set; }

        // Mối quan hệ 1-N với Xe
        public virtual ICollection<Xe> Xes { get; set; } = new List<Xe>();
    }
}