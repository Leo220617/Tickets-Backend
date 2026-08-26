using System.ComponentModel.DataAnnotations;

namespace Tickets.Models
{
    public class CorreoEnvioViewModel
    {
        public int id { get; set; }

        [StringLength(500)]
        public string RecepcionEmail { get; set; }

        [StringLength(500)]
        public string RecepcionPassword { get; set; }

        [StringLength(50)]
        public string RecepcionHostName { get; set; }

        public bool RecepcionUseSSL { get; set; }

        public int? EnvioPort { get; set; }

    }
}
