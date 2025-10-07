// Models/DTOs/DashboardTecnicoDto.cs
namespace SkyNet.Models.DTOs
{
    public class DashboardTecnicoDto
    {
        // --- KPIs ---
        public int Asignadas { get; set; }
        public int EnProceso { get; set; }
        public int Finalizadas { get; set; }
        public int Total { get; set; }

        // --- Listado ---
        public long Id { get; set; }
        public long IdSolicitud { get; set; }
        public int IdGrupo { get; set; }
        public long FkTecnico { get; set; }
        public DateTime? FechaAsignacionUtc { get; set; }
        public DateTime? Fecha_Inicio { get; set; }
        public DateTime? Fecha_Fin { get; set; }
        public string? Notas { get; set; }
        public int Estado { get; set; }
        public string? EstadoTexto { get; set; }
        public string? TecnicoNombre { get; set; }
        public string? SupervisorNombre { get; set; }
        public string? GrupoEtiqueta { get; set; }
        public string? Tipo { get; set; }
        public string? Prioridad { get; set; }

    }
}
