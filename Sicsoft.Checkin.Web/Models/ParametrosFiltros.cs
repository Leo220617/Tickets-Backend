using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InversionGloblalWeb.Models
{
    public class ParametrosFiltros
    {
        public string Texto { get; set; }
        public int Codigo1 { get; set; }
        public int Codigo2 { get; set; }
        public int Codigo3 { get; set; }

        public DateTime FechaInicial { get; set; }
        public DateTime FechaFinal { get; set; }

        public bool Asignados { get; set; }
        public string Estado { get; set; }
        public string CodMoneda { get; set; }
        public bool RegimeSimplificado { get; set; }
        public bool FacturaExterior { get; set; }
        public bool FacturaNoRecibida { get; set; }
        public int NumCierre { get; set; }
        public string Texto2 { get; set; }
    }
}
