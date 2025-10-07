using SkyNet.Models;

public class SolicitudTracking
{
    public long IdTracking { get; set; }             
    public long FkSolicitud { get; set; }             
    public string UserId { get; set; } = null!;      
    public byte? Estado { get; set; }             
    public DateTime FechaUtc { get; set; }          

   
    public Solicitud? Solicitud { get; set; }
}
