using InversionGloblalWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Refit;
using Sicsoft.Checkin.Web.Servicios;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Tickets.Models;

namespace Sicsoft.Checkin.Web.Pages
{

    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> logger;
        private readonly ICrudApi<DashboardTicketsViewModel, int> dashboardService;
        private readonly ICrudApi<UsuariosViewModel, int> users;

        [BindProperty(SupportsGet = true)]
        public ParametrosFiltros filtro { get; set; }

        public UsuariosViewModel[] Usuarios { get; set; }
        public DashboardTicketsViewModel Dashboard { get; set; }
        public bool PuedeFiltrarUsuario { get; set; }

        public IndexModel(
            ILogger<IndexModel> logger,
            ICrudApi<DashboardTicketsViewModel, int> dashboardService,
            ICrudApi<UsuariosViewModel, int> users)
        {
            this.logger = logger;
            this.dashboardService = dashboardService;
            this.users = users;

            filtro = new ParametrosFiltros();
            Usuarios = new UsuariosViewModel[0];
            Dashboard = new DashboardTicketsViewModel();
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var identity = User.Identity as ClaimsIdentity;
                var rolesClaim = identity?.Claims
                    .FirstOrDefault(x => x.Type == "Roles")?.Value ?? "";

                var roles = rolesClaim.Split(
                    new[] { '|' },
                    StringSplitOptions.RemoveEmptyEntries
                );

                PuedeFiltrarUsuario = roles.Contains("4");

                if (filtro == null)
                {
                    filtro = new ParametrosFiltros();
                }

                var hoy = DateTime.Today;

                if (filtro.FechaInicial == DateTime.MinValue)
                {
                    filtro.FechaInicial = new DateTime(hoy.Year, 1, 1);
                }

                if (filtro.FechaFinal == DateTime.MinValue)
                {
                    filtro.FechaFinal = hoy;
                }

                if (string.IsNullOrWhiteSpace(filtro.Texto3))
                {
                    filtro.Texto3 = "N";
                }

                if (!PuedeFiltrarUsuario)
                {
                    var idClaim = identity?.Claims
                        .FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)
                        ?.Value;

                    int idUsuario;
                    if (!int.TryParse(idClaim, out idUsuario))
                    {
                        return RedirectToPage("/NoPermiso");
                    }

                    filtro.Codigo1 = idUsuario;
                }

                var tareaDashboard = dashboardService.ObtenerLista(filtro);
                var tareaUsuarios = users.ObtenerLista("");

                await Task.WhenAll(tareaDashboard, tareaUsuarios);

                var resultado = await tareaDashboard;

                Dashboard = resultado?.FirstOrDefault()
                    ?? new DashboardTicketsViewModel();

                Usuarios = await tareaUsuarios
                    ?? new UsuariosViewModel[0];

                return Page();
            }
            catch (ApiException ex)
            {
                logger.LogError(ex, "No fue posible cargar el dashboard de tiquetes.");

                Dashboard = new DashboardTicketsViewModel();
                Usuarios = Usuarios ?? new UsuariosViewModel[0];

                var mensaje = ex.Message;

                if (!string.IsNullOrWhiteSpace(ex.Content))
                {
                    try
                    {
                        var error = JsonConvert.DeserializeObject<Errores>(ex.Content);
                        mensaje = error?.Message ?? ex.Content;
                    }
                    catch
                    {
                        mensaje = ex.Content;
                    }
                }

                ModelState.AddModelError(string.Empty, mensaje);
                return Page();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error cargando el dashboard de tiquetes.");
                Dashboard = new DashboardTicketsViewModel();
                Usuarios = Usuarios ?? new UsuariosViewModel[0];

                ModelState.AddModelError(
                    string.Empty,
                    "No fue posible cargar el dashboard. " + ex.Message
                );

                return Page();
            }
        }
    }
}

