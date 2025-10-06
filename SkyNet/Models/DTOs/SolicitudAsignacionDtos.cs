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
    public class SolicitudFinalizarDto
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


    [Keyless]
    public class SolicitudAsignacionDetalleDto
    {
        // Solicitud
        public long Id { get; set; }
        public long IdSolicitud { get; set; }
        public string Nombre { get; set; } = "";
        public string Email { get; set; } = "";
        public string? Telefono { get; set; }
        public string Tipo { get; set; } = "";
        public string Prioridad { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public string Ticket { get; set; } = "";
        public DateTime CreatedAtUtc { get; set; }
        public int EstadoSolicitud { get; set; }
        public string? Direccion { get; set; }
        public double? Latitud { get; set; }
        public double? Longitud { get; set; }

        // Asignación
        public long AsignacionId { get; set; }
        public int EstadoAsignacion { get; set; }
        public DateTime? AsignacionCreadaUtc { get; set; }
        public DateTime? AsignacionFechaInicio { get; set; }
        public DateTime? AsignacionFechaFin { get; set; }
        public string? AsignacionNotas { get; set; }
        public long IdGrupo { get; set; }
        public long FkTecnico { get; set; }

        // Aliases para compatibilidad con la vista
        public DateTime? Fecha_Inicio { get; set; }
        public DateTime? Fecha_Fin { get; set; }
        public string? Notas { get; set; }
        public int Estado { get; set; }

        // Extras
        public string? GrupoEtiqueta { get; set; }
        public string? TecnicoNombre { get; set; }
        public string? SupervisorNombre { get; set; }
    }





}
