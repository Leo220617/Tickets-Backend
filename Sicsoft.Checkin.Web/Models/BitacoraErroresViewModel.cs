using System;

namespace Tickets.Models
{
    public class BitacoraErroresViewModel
    {
        public int id { get; set; }
        public string Descripcion { get; set; }
        public string StackTrace { get; set; }
        public DateTime Fecha { get; set; }
        public string JSON { get; set; }
    }
}
