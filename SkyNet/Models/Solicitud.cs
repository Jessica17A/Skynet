using System.ComponentModel.DataAnnotations;

namespace SkyNet.Models
{
    public enum SolicitudEstado { Pendiente = 0, EnProceso = 1, Cerrada = 2 }

    public class Solicitud
    {
        public long Id { get; set; }

        [Required, StringLength(120)]
        public string Nombre { get; set; } = "";

        [Required, EmailAddress, StringLength(160)]
        public string Email { get; set; } = "";

        [StringLength(30)]
        public string? Telefono { get; set; }

        [Required, StringLength(60)]
        public string Tipo { get; set; } = "";

        [StringLength(20)]
        public string Prioridad { get; set; } = "";

        [Required, StringLength(1500)]
        public string Descripcion { get; set; } = "";

        // Ticket & estado
        [Required, StringLength(40)]
        public string Ticket { get; set; } = "";

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public SolicitudEstado Estado { get; set; } = SolicitudEstado.Pendiente;

        // Cloudinary (opcional)
        public string? AdjuntoPublicId { get; set; }

        // Dirección / Geo
        [StringLength(300)]
        public string? Direccion { get; set; }
        public double? Latitud { get; set; }
        public double? Longitud { get; set; }
    }
}
