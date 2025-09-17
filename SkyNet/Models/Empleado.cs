namespace SkyNet.Models;

public class Empleado
{
    public long Id { get; set; }         // PK
    public string? Nombre { get; set; }
   public string? DPI { get; set; }

    public string Direccion { get; set; } = null!;
    public string Telefono { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Cargo { get; set; } 
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public int Estado { get; set; } // 1 activo, 0 inactivo
}
