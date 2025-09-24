using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SkyNet.Models
{
    public class GrupoCreateVm
    {
        [Required(ErrorMessage = "Seleccione un supervisor.")]
        public long SupervisorId { get; set; }

        [Required(ErrorMessage = "Seleccione al menos un técnico.")]
        [MinLength(1, ErrorMessage = "Seleccione al menos un técnico.")]
        public List<long> TecnicosIds { get; set; } = new();
    }

}
