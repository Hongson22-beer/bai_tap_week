using System;

namespace BtlThueXe.Core.DTOs.Contracts
{
    public class ContractHistoryDto
    {
        public int Id { get; set; }

        public int IdHopDong { get; set; }

        public string? TrangThaiCu { get; set; }

        public string TrangThaiMoi { get; set; } = string.Empty;

        public int? IdNguoiThayDoi { get; set; }

        public string? LyDo { get; set; }

        public DateTime ThoiGianThayDoi { get; set; }
    }
}