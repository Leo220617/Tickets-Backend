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
    public class EditarModel : PageModel
    {
        private readonly IConfiguration configuration;
        private readonly ICrudApi<TiquetesViewModel, int> service;
        private readonly ICrudApi<UsuariosViewModel, int> users;
        private readonly ICrudApi<EmpresasViewModel, int> serviceE;
        private readonly ICrudApi<Adjuntos, int> serviceAdj;
        [BindProperty]
        public TiquetesViewModel Tiquete { get; set; }
        [BindProperty]
        public UsuariosViewModel[] Usuarios { get; set; }
        [BindProperty]
        public EmpresasViewModel[] Empresas { get; set; }

        [BindProperty]
        public Adjuntos[] Adj { get; set; }

        public EditarModel(ICrudApi<TiquetesViewModel, int> service, ICrudApi<UsuariosViewModel, int> users, ICrudApi<EmpresasViewModel, int> serviceE, ICrudApi<Adjuntos, int> serviceAdj)
        {
            this.service = service;
            this.users = users;
            this.serviceE = serviceE;
            this.serviceAdj = serviceAdj;
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

                if(id != 0)
                {
                    ParametrosFiltros filtro = new ParametrosFiltros();

                    filtro.Codigo1 = id;
                    Adj = await serviceAdj.ObtenerLista(filtro);
                }
                
                Empresas = await serviceE.ObtenerLista("");
                Usuarios = await users.ObtenerLista("");
                Tiquete = await service.ObtenerPorId(id);
                if (id != 0)
                {
                        foreach (var item in Adj)
                    {
                        Tiquete.Adjunto += item.Adjunto + "¶";
                    }
                }

                Tiquete.DuracionReal = Tiquete.Duracion;

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
                if(Tiquete.Adjunto != null)
                {
                    var array = Tiquete.Adjunto.Split("¶");
                    List<Adjuntos> arrayAdj = new List<Adjuntos>();
                    foreach (var item in array)
                    {
                        Adjuntos ad = new Adjuntos();
                        ad.idTicket = Tiquete.id;
                        ad.Adjunto = item;
                        arrayAdj.Add(ad);
                    }
                     await serviceAdj.AgregarBulk(arrayAdj.ToArray());
                }
               if(Tiquete.Duracion == "00:00:00" || (Tiquete.Duracion != Tiquete.DuracionReal && Convert.ToDateTime(Tiquete.DuracionReal) > Convert.ToDateTime(Tiquete.Duracion)))
                {
                    Tiquete.Duracion = Tiquete.DuracionReal;
                }
                await service.Editar(Tiquete);
                return RedirectToPage("./Index");
            }
            catch (ApiException ex)
            {
                
                ModelState.AddModelError(string.Empty, ex.Content.ToString());
                return Page();
            }
        }
    }
}
