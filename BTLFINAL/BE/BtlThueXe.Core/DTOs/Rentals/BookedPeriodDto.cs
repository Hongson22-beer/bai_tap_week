using System;

namespace BtlThueXe.Core.DTOs.Rentals
{
    public class BookedPeriodDto
    {
        public int RentalId { get; set; }

        public DateTime Start { get; set; }

        public DateTime End { get; set; }

        public string TrangThai { get; set; } = string.Empty;
    }
}