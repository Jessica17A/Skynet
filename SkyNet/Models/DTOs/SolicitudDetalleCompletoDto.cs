using Microsoft.EntityFrameworkCore;

namespace SkyNet.Models.DTOs
{

    [Keyless]
    public class SolicitudDetalleCompletoDto
    {
        public long IdSolicitud { get; set; }
        public string Nombre { get; set; }
        public string Email { get; set; }
        public string Telefono { get; set; }
        public string Tipo { get; set; }
        public string Prioridad { get; set; }
        public string Descripcion { get; set; }
        public string Ticket { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public int EstadoSolicitud { get; set; }
        public string Direccion { get; set; }
        public string TecnicoNombre { get; set; }
        public string SupervisorNombre { get; set; }
        public DateTime? AsignacionFechaInicio { get; set; }
        public DateTime? AsignacionFechaFin { get; set; }
        public string AsignacionNotas { get; set; }
    }
}
