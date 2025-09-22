// Models/Usuarios/UsuarioListadoVm.cs
namespace SkyNet.Models.Usuarios
{
    public class UsuarioListadoVm
    {
        public string UserId { get; set; } = "";
        public string UserName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Roles { get; set; } = "";           // "Admin, Soporte"

        public long? EmpleadoId { get; set; }              // null si no está enlazado
        public string? Nombres { get; set; }
        public string? Apellidos { get; set; }
        public string? Cargo { get; set; }
        public int? Estado { get; set; }                  // 1 Activo, 0 Inactivo (según tu tabla)
        public string NombreCompleto => $"{Nombres} {Apellidos}".Trim();
    }
}
