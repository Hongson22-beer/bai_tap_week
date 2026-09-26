using System;
using System.Collections.Generic;


namespace BtlThueXe.Infrastructure;

public partial class HangXe
{
    public int Id { get; set; }

    public string Ten { get; set; } = null!;

    public string? QuocGia { get; set; }

    public string? MoTa { get; set; }

    public virtual ICollection<Xe> Xes { get; set; } = new List<Xe>();
}
