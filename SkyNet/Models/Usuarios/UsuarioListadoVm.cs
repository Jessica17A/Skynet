// Models/Usuarios/UsuarioListadoVm.cs
namespace SkyNet.Models.Usuarios
{
    public class UsuarioListadoVm
    {
        public string UserId { get; set; } = "";
        public string UserName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Roles { get; set; } = "";          

        public long? EmpleadoId { get; set; }         
        public string? Nombres { get; set; }
        public string? Apellidos { get; set; }
        public string? Cargo { get; set; }
        public int? Estado { get; set; }                 
        public string NombreCompleto => $"{Nombres} {Apellidos}".Trim();
    }
}
