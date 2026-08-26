using System;
using System.Collections.Generic;

namespace Tickets.Models
{
    public class DashboardTicketsViewModel
    {
        public int Total { get; set; }
        public int Abiertos { get; set; }
        public int Validacion { get; set; }
        public int Espera { get; set; }
        public int Cerrados { get; set; }
        public int SinAsignar { get; set; }
        public decimal HorasInvertidas { get; set; }
        public decimal PromedioHoras { get; set; }
        public decimal TasaResolucion { get; set; }
        public decimal CumplimientoEstimado { get; set; }
        public decimal EdadPromedioActivos { get; set; }

        public List<DashboardCategoriaViewModel> Estados { get; set; }
            = new List<DashboardCategoriaViewModel>();

        public List<DashboardCategoriaViewModel> Tipos { get; set; }
            = new List<DashboardCategoriaViewModel>();

        public List<DashboardCategoriaViewModel> Antiguedad { get; set; }
            = new List<DashboardCategoriaViewModel>();

        public List<DashboardTendenciaViewModel> Tendencia { get; set; }
            = new List<DashboardTendenciaViewModel>();

        public List<DashboardConsultorViewModel> Consultores { get; set; }
            = new List<DashboardConsultorViewModel>();

        public List<DashboardEmpresaViewModel> Empresas { get; set; }
            = new List<DashboardEmpresaViewModel>();

        public List<DashboardTicketRecienteViewModel> Recientes { get; set; }
            = new List<DashboardTicketRecienteViewModel>();
    }

    public class DashboardCategoriaViewModel
    {
        public string Etiqueta { get; set; }
        public int Valor { get; set; }
    }

    public class DashboardTendenciaViewModel
    {
        public int Anno { get; set; }
        public int Mes { get; set; }
        public string Etiqueta { get; set; }
        public int Creados { get; set; }
        public int Cerrados { get; set; }
    }

    public class DashboardConsultorViewModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public int Total { get; set; }
        public int Activos { get; set; }
        public int Cerrados { get; set; }
        public decimal Horas { get; set; }
    }

    public class DashboardEmpresaViewModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public int Total { get; set; }
        public int Activos { get; set; }
        public int Cerrados { get; set; }
    }

    public class DashboardTicketRecienteViewModel
    {
        public int Id { get; set; }
        public DateTime? Fecha { get; set; }
        public string Asunto { get; set; }
        public string Estado { get; set; }
        public string EstadoCodigo { get; set; }
        public string Tipo { get; set; }
        public string Usuario { get; set; }
        public string Empresa { get; set; }
    }
}
