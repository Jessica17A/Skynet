using System;

namespace SkyNet.Models.DTOs
{
    // Lectura / detalle
    public class EmpleadoDTO
    {
        public long Id { get; set; }
        public string? Nombre { get; set; }
        public string? DPI { get; set; }

        public string Direccion { get; set; } = default!;
        public string Telefono { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string? Cargo { get; set; }

        public DateTime CreatedAtUtc { get; set; }
        public int Estado { get; set; } // 1 activo, 0 inactivo
    }

    // Crear (todo opcional)
    public class EmpleadoCrearDTO
    {
        public string? Nombre { get; set; }
        public string? DPI { get; set; }
        public string? Direccion { get; set; }
        public string? Telefono { get; set; }
        public string? Email { get; set; }
        public string? Cargo { get; set; }
        public int? Estado { get; set; }   // null = usa el default del servidor (p.ej. 1)
    }

    // Editar (patch/put parcial, todo opcional)
    public class EmpleadoEditarDTO
    {
        public string? Nombre { get; set; }
        public string? DPI { get; set; }
        public string? Direccion { get; set; }
        public string? Telefono { get; set; }
        public string? Email { get; set; }
        public string? Cargo { get; set; }
        public int? Estado { get; set; }
    }
}
