using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Tickets.Models
{
    public class TiquetesViewModel
    {
        public int id { get; set; }

        public DateTime? FechaTicket { get; set; }

        [StringLength(200)]
        [Required]
        public string Asunto { get; set; }

        [Required]
        public string Mensaje { get; set; }

        public string Comentarios { get; set; }

        public int? idLoginAsignado { get; set; }

        [StringLength(10)]
        public string Duracion { get; set; }

        [StringLength(500)]
        public string PersonaTicket { get; set; }
        public string Status { get; set; }
        public int idEmpresa { get; set; }

        public string Adjunto { get; set; }
        public string DuracionEstimada { get; set; }
    }
}
