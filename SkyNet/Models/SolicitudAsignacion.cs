// Models/SolicitudAsignacion.cs
using System;

namespace SkyNet.Models
{
    public enum SolicitudAsignacionEstado : byte
    {
        Activa = 1,     // asignación vigente
        Finalizada = 2, // cuando la solicitud cierra o se completa el trabajo
        Anulada = 0     // se anuló/reemplazó la asignación
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

        // Quién asignó (opcional)
        public string? AsignadoPorUserId { get; set; }

        // Notas opcionales
        public string? Notas { get; set; }

        public SolicitudAsignacionEstado Estado { get; set; } = SolicitudAsignacionEstado.Activa;

       
    }
}
