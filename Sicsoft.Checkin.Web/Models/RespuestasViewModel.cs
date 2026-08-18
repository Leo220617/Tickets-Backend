using System;

namespace Tickets.Models
{
    public class RespuestasViewModel
    {
        public int id { get; set; }
        public int idTicket { get; set; }
        public int idUsuario { get; set; }
        public string Respuesta { get; set; }
        public bool EsNotaInterna { get; set; }
        public DateTime FechaCreacion { get; set; }
    }
}
