namespace BtlThueXe.Core.DTOs.Vehicles;

public class BrandDto
{
    public int Id { get; set; }
    public string Ten { get; set; } = string.Empty;
    public string? QuocGia { get; set; }
}

public class CreateBrandDto
{
    public string Ten { get; set; } = string.Empty;
    public string? QuocGia { get; set; }
}

public class VehicleTypeDto
{
    public int Id { get; set; }
    public string Ten { get; set; } = string.Empty;
    public int SoCho { get; set; }
    public string? MoTa { get; set; }
}

public class CreateVehicleTypeDto
{
    public string Ten { get; set; } = string.Empty;
    public int SoCho { get; set; }
    public string? MoTa { get; set; }
}