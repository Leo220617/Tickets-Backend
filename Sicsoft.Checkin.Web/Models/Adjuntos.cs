using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tickets.Models
{
    public class Adjuntos
    {
        public int id { get; set; }
        public int idTicket { get; set; }
        public string Adjunto { get; set; }
    }
}
