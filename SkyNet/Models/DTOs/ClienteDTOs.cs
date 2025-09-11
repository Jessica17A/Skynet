namespace SkyNet.Models.DTOs;

public class ClienteDTO
{
    public long Id { get; set; }
    public string? Nombre { get; set; }
    public string? Email  { get; set; }
}

public class ClienteCrearDTO
{
    public string? Nombre { get; set; }
    public string? Email  { get; set; }
}

public class ClienteEditarDTO
{
    public string? Nombre { get; set; }
    public string? Email  { get; set; }
}


