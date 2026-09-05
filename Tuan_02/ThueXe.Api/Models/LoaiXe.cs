namespace ThueXe.Api.Models;

public class LoaiXe
{
    public int Id { get; set; }
    public string MaLoai { get; set; } = string.Empty;
    public string TenLoai { get; set; } = string.Empty;
    public int SoCho { get; set; }
    public string? MoTa { get; set; }

    public ICollection<Xe> Xes { get; set; } = new List<Xe>();
}