using System.ComponentModel.DataAnnotations;

namespace SkyNet.Models.DTOs
{
    public class SolicitudCreateDto
    {
        [Required, StringLength(120)]
        public string Nombre { get; set; } = "";

        [Required, EmailAddress, StringLength(160)]
        public string Email { get; set; } = "";

        [StringLength(30)]
        public string? Telefono { get; set; }

        [Required, StringLength(60)]
        public string Tipo { get; set; } = "";

        [Required, StringLength(30)]
        public string Prioridad { get; set; } = "";


        [Required, StringLength(1500)]
        public string Descripcion { get; set; } = "";

        // archivo
        public IFormFile? Adjunto { get; set; }

        // geo
        public string? Direccion { get; set; }
        public double? Latitud { get; set; }
        public double? Longitud { get; set; }
    }

    public class SolicitudDto
    {
        public long Id { get; set; }
        public string Nombre { get; set; } = "";
        public string Email { get; set; } = "";
        public string? Telefono { get; set; }
        public string Tipo { get; set; } = "";
        public string Prioridad { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public string Ticket { get; set; } = "";
        public DateTime CreatedAtUtc { get; set; }
        public SkyNet.Models.SolicitudEstado Estado { get; set; }
        public string? AdjuntoPublicId { get; set; }
        public string? Direccion { get; set; }
        public double? Latitud { get; set; }
        public double? Longitud { get; set; }
    }




}
