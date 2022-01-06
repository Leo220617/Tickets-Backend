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
using Sicsoft.Checkin.Web.Servicios;
using Tickets.Models;

namespace Tickets.Pages.Tiquetes
{
    public class NuevoModel : PageModel
    {
        private readonly IConfiguration configuration;
        private readonly ICrudApi<TiquetesViewModel, int> service;
        private readonly ICrudApi<UsuariosViewModel, int> users;
        private readonly ICrudApi<EmpresasViewModel, int> serviceE;

        [BindProperty]
        public TiquetesViewModel Tiquete { get; set; }
        [BindProperty]
        public UsuariosViewModel[] Usuarios { get; set; }
        [BindProperty]
        public EmpresasViewModel[] Empresas { get; set; }

        public NuevoModel(ICrudApi<TiquetesViewModel, int> service, ICrudApi<UsuariosViewModel, int> users, ICrudApi<EmpresasViewModel, int> serviceE)
        {
            this.service = service;
            this.users = users;
            this.serviceE = serviceE;
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            try
            {
                var Roles = ((ClaimsIdentity)User.Identity).Claims.Where(d => d.Type == "Roles").Select(s1 => s1.Value).FirstOrDefault().Split("|");
                if (string.IsNullOrEmpty(Roles.Where(a => a == "1").FirstOrDefault()))
                {
                    return RedirectToPage("/NoPermiso");
                }
                Tiquete = new TiquetesViewModel();
                Tiquete.PersonaTicket = ((ClaimsIdentity)User.Identity).Claims.Where(d => d.Type == ClaimTypes.Name).Select(s1 => s1.Value).FirstOrDefault();
                Tiquete.FechaTicket = DateTime.Now;
                Empresas = await serviceE.ObtenerLista("");
                Usuarios = await users.ObtenerLista("");
                
                return Page();
            }
            catch (ApiException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                

                await service.Agregar(Tiquete);
                return RedirectToPage("./Index");
            }
            catch (ApiException ex)
            {
                Errores error = JsonConvert.DeserializeObject<Errores>(ex.Content.ToString());
                ModelState.AddModelError(string.Empty, error.Message);
                return Page();
            }
        }


    }
}
