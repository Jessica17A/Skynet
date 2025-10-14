namespace SkyNet.Models.DTOs
{
    public class UsuarioDto
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

    public class UsuarioEstadoUpdateDto
    {
        // 1 = Activo, 0 = Inactivo
        public int Estado { get; set; }
    }

    public class UsuarioResetDto
    {
        public string NewPassword { get; set; } = string.Empty;
    }

    public class ResetPasswordRequest
    {
        public string Id { get; set; } = "";
        public string NewPassword { get; set; } = "";
    }

    public class ResetPasswordDto
    {
        public string NewPassword { get; set; } = string.Empty;
    }




}
