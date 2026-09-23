namespace BtlThueXe.Core.DTOs.Reports;

public class ReportSummaryResponse
{
    public int TongNguoiDung { get; set; }
    public int TongKhachHang { get; set; }
    public int TongXe { get; set; }
    public int XeAvailable { get; set; }
    public int XeReserved { get; set; }
    public int XeRenting { get; set; }
    public int XeMaintenance { get; set; }
    public int TongYeuCauThue { get; set; }
    public int YeuCauPending { get; set; }
    public int TongHopDong { get; set; }
    public int HopDongInProgress { get; set; }
    public int HopDongCompleted { get; set; }
    public decimal TongTienDaThanhToan { get; set; }
    public decimal TongTienDaHoan { get; set; }
}
