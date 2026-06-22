using iTextSharp.text;
using System;
using System.Collections.Generic;

namespace Tickets.Models
{
    public class ActividadesViewModel
    {
        public int id { get; set; }

        public int idUsuario { get; set; }
        public int idEmpresa { get; set; }
        public int idTipoActividad { get; set; }

        public string titulo { get; set; }
        public DateTime fechaAgendada { get; set; }
        public string comentario { get; set; }

        public DateTime fechaCreacion { get; set; }

        public string estado { get; set; }
         
        public bool tieneAdjunto { get; set; }

        public string title { get; set; }
        public DateTime start { get; set; }
        public extendedProps extendedProps { get; set; }
        public List<AdjuntoActividadViewModel> adjuntos_actividades { get; set; }
    }

    public class extendedProps
    {
        public int tipo { get; set; }
        public string estado { get; set; }
        public string comentario { get; set; }
        public bool tieneAdjunto { get; set; }
        public string NomActividad { get; set; }
        public DateTime start { get; set; }

        public List<AdjuntoActividadViewModel> adjuntos { get; set; }


    }
}
