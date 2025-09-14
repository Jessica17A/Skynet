using System.ComponentModel.DataAnnotations;
using SkyNet.Models;

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

        [StringLength(20)]
        public string Prioridad { get; set; } = "Normal";

        [Required, StringLength(1500)]
        public string Descripcion { get; set; } = "";

        public IFormFile? Adjunto { get; set; }
    }

    public class SolicitudDto
    {
        public long Id { get; set; }
        public string Nombre { get; set; } = "";
        public string Email { get; set; } = "";
        public string? Telefono { get; set; }
        public string Tipo { get; set; } = "";
        public string Prioridad { get; set; } = "Normal";
        public string Descripcion { get; set; } = "";
        public string Ticket { get; set; } = "";
        public DateTime CreatedAtUtc { get; set; }
        public SolicitudEstado Estado { get; set; }
        public string? AdjuntoPath { get; set; }
        public string? AdjuntoPublicId { get; internal set; }
    }
}
