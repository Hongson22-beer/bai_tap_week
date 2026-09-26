namespace BtlThueXe.Core.DTOs.Returns;

public class CreateReturnIntentRequest
{
    public string? GhiChu { get; set; }
}

public class ReturnIntentResponse
{
    public int Id { get; set; }
    public int IdHopDong { get; set; }
    public int IdKhachHang { get; set; }
    public string? GhiChu { get; set; }
    public string TrangThai { get; set; } = string.Empty;
    public DateTime ThoiGianYeuCau { get; set; }
    public DateTime? ThoiGianXuLy { get; set; }
}
