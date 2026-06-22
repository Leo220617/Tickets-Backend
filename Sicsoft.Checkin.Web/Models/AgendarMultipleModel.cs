using System;
using System.Collections.Generic;

namespace Tickets.Models
{
    public class AgendarMultipleModel
    {
        public List<AdjuntoActividadViewModel> adjuntos_actividades { get; set; }
        public List<string> clientes { get; set; }
        public DateTime fecha { get; set; }
        public string tipo { get; set; }

        public string titulo { get; set; }
    }
}
