using BtlThueXe.Core.DTOs.Reports;
using BtlThueXe.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BtlThueXe.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Roles = "ADMIN")]
public class ReportsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ReportsController(ApplicationDbContext context) => _context = context;

    [HttpGet("summary")]
    public async Task<ActionResult<ReportSummaryResponse>> GetSummary()
    {
        var result = new ReportSummaryResponse
        {
            TongNguoiDung = await _context.NguoiDungs.CountAsync(),
            TongKhachHang = await _context.KhachHangs.CountAsync(),
            TongXe = await _context.Xes.CountAsync(),
            XeAvailable = await _context.Xes.CountAsync(x => x.TrangThai == "AVAILABLE"),
            XeReserved = await _context.Xes.CountAsync(x => x.TrangThai == "RESERVED"),
            XeRenting = await _context.Xes.CountAsync(x => x.TrangThai == "RENTING"),
            XeMaintenance = await _context.Xes.CountAsync(x => x.TrangThai == "MAINTENANCE"),
            TongYeuCauThue = await _context.YeuCauThues.CountAsync(),
            YeuCauPending = await _context.YeuCauThues.CountAsync(x => x.TrangThai == "PENDING"),
            TongHopDong = await _context.HopDongs.CountAsync(),
            HopDongInProgress = await _context.HopDongs.CountAsync(x => x.TrangThai == "IN_PROGRESS"),
            HopDongCompleted = await _context.HopDongs.CountAsync(x => x.TrangThai == "COMPLETED"),
            TongTienDaThanhToan = await _context.ThanhToans
                .Where(x => x.TrangThai == "PAID" && x.LoaiThanhToan != "HOAN_TIEN")
                .SumAsync(x => (decimal?)x.SoTien) ?? 0m,
            TongTienDaHoan = await _context.ThanhToans
                .Where(x => x.TrangThai == "REFUNDED" && x.LoaiThanhToan == "HOAN_TIEN")
                .SumAsync(x => (decimal?)x.SoTien) ?? 0m
        };

        return Ok(result);
    }
}
