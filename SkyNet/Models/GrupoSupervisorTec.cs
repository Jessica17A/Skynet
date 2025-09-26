// Models/GrupoSupervisorTec.cs
namespace SkyNet.Models
{
    public class Empleados
    {
        public long Id { get; set; }     // bigint -> long
        public string Nombres { get; set; } = "";
        public string Apellidos { get; set; } = "";
        public string? Cargo { get; set; }
        public bool Estado { get; set; }
    }

    public class GrupoSupervisorTec
    {
        public int IdGrupo { get; set; }         // IDGRUPO sigue siendo int
        public long FkSupervisor { get; set; }   // bigint -> long
        public long FkTecnico { get; set; }      // bigint -> long
        public DateTime FechaCreacionUtc { get; set; } = DateTime.UtcNow;
        public bool Estado { get; set; } = true;

        public Empleado? Supervisor { get; set; }
        public Empleado? Tecnico { get; set; }
    }

}
