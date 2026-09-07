using System;
using System.Collections.Generic;

namespace BtlThueXe.Infrastructure;

public partial class VaiTro
{
    public int Id { get; set; }

    public string Ten { get; set; } = null!;

    public string? MoTa { get; set; }

    public virtual ICollection<NguoiDung> IdNguoiDungs { get; set; } = new List<NguoiDung>();

    public virtual ICollection<Quyen> IdQuyens { get; set; } = new List<Quyen>();
}
