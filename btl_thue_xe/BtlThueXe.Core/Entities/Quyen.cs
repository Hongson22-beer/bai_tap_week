using System;
using System.Collections.Generic;

namespace BtlThueXe.Core.Entities;

public partial class Quyen
{
    public int Id { get; set; }

    public string MaQuyen { get; set; } = null!;

    public string? MoTa { get; set; }

    public virtual ICollection<VaiTro> IdVaiTros { get; set; } = new List<VaiTro>();
}
