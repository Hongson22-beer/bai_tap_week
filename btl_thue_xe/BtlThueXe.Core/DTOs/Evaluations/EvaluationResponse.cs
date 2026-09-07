namespace BtlThueXe.Core.DTOs.Evaluations;

public class EvaluationResponse
{
    public int Id { get; set; }

    public int IdHopDong { get; set; }

    public int IdKhachHang { get; set; }

    public int? IdXe { get; set; }

    public int DiemDanhGia { get; set; }

    public string? NhanXet { get; set; }

    public DateTime ThoiGianTao { get; set; }
}