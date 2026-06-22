using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using InversionGloblalWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Refit;
using Sicsoft.Checkin.Web.Models;

using Sicsoft.Checkin.Web.Servicios;
using Tickets.Models;

namespace Tickets.Pages.TiposActividades
{
    public class IndexModel : PageModel
    {
        private readonly IConfiguration configuration;
        private readonly ICrudApi<TipoActividadesViewModel, int> service;

        [BindProperty]
        public TipoActividadesViewModel[] Objeto { get; set; }
        [BindProperty(SupportsGet = true)]
        public ParametrosFiltros filtro { get; set; }

        public IndexModel(ICrudApi<TipoActividadesViewModel, int> service)
        {
            this.service = service;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var Roles1 = ((ClaimsIdentity)User.Identity).Claims.Where(d => d.Type == "Roles").Select(s1 => s1.Value).FirstOrDefault().Split("|");
                if (string.IsNullOrEmpty(Roles1.Where(a => a == "18").FirstOrDefault()))
                {
                    return RedirectToPage("/NoPermiso");
                }

                Objeto = await service.ObtenerLista(filtro);



                return Page();
            }
            catch (ApiException ex)
            {
                Errores error = JsonConvert.DeserializeObject<Errores>(ex.Content.ToString());
                ModelState.AddModelError(string.Empty, error.Message);

                return Page();
            }
        }

        public async Task<IActionResult> OnGetEliminar(int id)
        {
            try
            {

                await service.Eliminar(id);
                return new JsonResult(true);
            }
            catch (ApiException ex)
            {
                return new JsonResult(false);
            }
        }
    }
}
