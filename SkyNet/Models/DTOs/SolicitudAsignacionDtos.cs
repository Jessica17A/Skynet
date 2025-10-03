// Models/DTOs/SolicitudAsignacionDtos.cs
using Microsoft.EntityFrameworkCore;
using System;

namespace SkyNet.Models.DTOs
{
    // Para crear desde UI (POST)
    public class SolicitudAsignacionCreateDto
    {
        public long IdSolicitud { get; set; }
        public int IdGrupo { get; set; }
        public long FkTecnico { get; set; }
        public DateTime? Fecha_Inicio { get; set; }
        public string? Notas { get; set; }
      
    }

    // Para devolver a la UI
    public class SolicitudAsignacionDto
    {
        public long Id { get; set; }
        public long IdSolicitud { get; set; }
        public int IdGrupo { get; set; }
        public long FkTecnico { get; set; }
        public DateTime FechaAsignacionUtc { get; set; }
       
        public string? Notas { get; set; }
        public byte Estado { get; set; } 

        public DateTime? Fecha_Inicio { get; internal set; }
    }

    // Para cambiar estado (anular/finalizar)
    public class SolicitudAsignacionEstadoDto
    {
        public byte Estado { get; set; }  
        public string? Nota { get; set; }
        public DateTime? Fecha_Fin { get; set; }
    }


    [Keyless]
    public class SolicitudAsignacionListado // o usa tu SolicitudAsignacionDto si prefieres
    {
        public long Id { get; set; }
        public long IdSolicitud { get; set; }
        public int IdGrupo { get; set; }
        public long FkTecnico { get; set; }
        public DateTime FechaAsignacionUtc { get; set; }
        public DateTime? Fecha_Inicio { get; set; }
        public DateTime? Fecha_Fin { get; set; }
        public string? Notas { get; set; }
        public byte Estado { get; set; }
        public string? TecnicoNombre { get; set; }
        public string? SupervisorNombre { get; set; }
        public string? GrupoEtiqueta { get; set; }
    }


    public class SolicitudResumenDto
{
    public long IdSolicitud { get; set; }
    public DateTime? FechaVisita_Min { get; set; } // mapea FECHAVISITA_MIN
    public byte Estado_Agregado { get; set; }      // mapea ESTADO_AGREGADO
    public string? Supervisores { get; set; }
    public string? Tecnicos { get; set; }
    public string? Asignaciones_Json { get; set; } // mapea ASIGNACIONES_JSON
}

public class AsignacionItem
{
    public long Id { get; set; }
    public int IdGrupo { get; set; }
    public string? GrupoEtiqueta { get; set; }
    public long FkTecnico { get; set; }
    public string? TecnicoNombre { get; set; }
    public string? SupervisorNombre { get; set; }
    public DateTime FechaAsignacionUtc { get; set; }
    public DateTime? Fecha_Inicio { get; set; }
    public DateTime? Fecha_Fin { get; set; }
    public byte Estado { get; set; }
    public string? Notas { get; set; }
}

}
