// Models/SolicitudAsignacion.cs
using System;

namespace SkyNet.Models
{
    public enum SolicitudAsignacionEstado : byte
    {
        Activa = 1,     
        Finalizada = 2, 
        Anulada = 0,
        Asignada = 3,
        Proceso = 4
    }

    public class SolicitudAsignacion
    {
        public long Id { get; set; }

        // FK hacia la solicitud
        public long FkSolicitud { get; set; }

        // # de grupo (tu tabla Grupos_Supervisores_Tec trabaja con IdGrupo int)
        public int IdGrupo { get; set; }

        // ID del técnico (ajusta el tipo si en tu sistema es string/Guid)
        public long FkTecnico { get; set; }

        public DateTime FechaAsignacionUtc { get; set; } = DateTime.UtcNow;

        public DateTime? Fecha_Inicio { get; set; }

        public DateTime? Fecha_Fin { get; set; }

        // Notas opcionales
        public string? Notas { get; set; }

        public SolicitudAsignacionEstado Estado { get; set; } = SolicitudAsignacionEstado.Activa;

       
    }
}
