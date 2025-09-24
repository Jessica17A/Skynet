using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SkyNet.Models.DTOs
{
    public class GrupoItemDto
    {
        public int IdGrupo { get; set; }
        public long SupervisorId { get; set; }
        public string SupervisorNombre { get; set; } = "";
        public long TecnicoId { get; set; }
        public string TecnicoNombre { get; set; } = "";
        public DateTime FechaCreacionUtc { get; set; }
        public bool Estado { get; set; }
    }

    public class GrupoCreateDto
    {
        [Required]
        public long SupervisorId { get; set; }

        [Required, MinLength(1)]
        public List<long> TecnicosIds { get; set; } = new();
    }

    public class OpcionEmpleadoDto
    {
        public long Id { get; set; }
        public string Nombre { get; set; } = "";
    }
}
