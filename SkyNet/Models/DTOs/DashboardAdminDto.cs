namespace SkyNet.Models.DTOs
{
    public class DashboardAdminDto
    {
        public int Rechazado { get; set; }
        public int Pendiente { get; set; }
        public int Revisada { get; set; }
        public int Asignadas { get; set; }
        public int EnProceso { get; set; }
        public int Finalizado { get; set; }
        public int Total { get; set; }
    }
}
