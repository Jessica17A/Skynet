using System.ComponentModel.DataAnnotations;

namespace SkyNet.Models
{
    public enum SolicitudEstado { Pendiente = 1, EnProceso = 4, Cerrada = 5 }

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

        [Required, StringLength(30)] // ← igual que en el DTO
        public string Prioridad { get; set; } = "";

        [Required, StringLength(1500)]
        public string Descripcion { get; set; } = "";

        [Required, StringLength(40)]
        public string Ticket { get; set; } = "";

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public SolicitudEstado Estado { get; set; } = SolicitudEstado.Pendiente;

        [StringLength(300)]
        public string? Direccion { get; set; }
        public double? Latitud { get; set; }
        public double? Longitud { get; set; }

        public ICollection<ArchivoSolicitud> Archivos { get; set; } = new List<ArchivoSolicitud>();
    }

    public class ArchivoSolicitud
    {
        public int Id { get; set; } 
        public long Fk_Solicitud { get; set; } 
        public Solicitud Solicitud { get; set; } = null!;

        [Required, StringLength(512)]
        public string PublicId { get; set; } = null!;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public int Estado = 1;
    }
}
