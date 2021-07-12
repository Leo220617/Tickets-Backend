using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using InversionGloblalWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Refit;
using Sicsoft.Checkin.Web.Models;

using Sicsoft.Checkin.Web.Servicios;

namespace InversionGloblalWeb.Pages.SeguridadRolesModulos
{
    [AllowAnonymous]
    [DisableRequestSizeLimit]
    [RequestSizeLimit(long.MaxValue)]
    public class RolesModulosModel : PageModel
    {
        private readonly IConfiguration configuration;
        private readonly ICrudApi<RolesViewModel, int> roles;
        private readonly ICrudApi<SeguridadModulosViewModel, int> modulos;
        private readonly ICrudApi<SeguridadRolesModulosViewModel, int> rolesMod;


        [BindProperty]
        public RolesViewModel Roles { get; set; }

        [BindProperty]
        public SeguridadRolesModulosViewModel[] ModulosMios { get; set; }

        [BindProperty]
        public SeguridadRolesModulosViewModel[] Modulos { get; set; }

        [BindProperty]
        public SeguridadModulosViewModel[] ModulosMiosS { get; set; }

        [BindProperty]
        public SeguridadModulosViewModel[] ModulosS { get; set; }

        [BindProperty(SupportsGet = true)]
        public ParametrosFiltros filtro { get; set; }

        public RolesModulosModel(ICrudApi<RolesViewModel, int> roles, ICrudApi<SeguridadModulosViewModel, int> modulos, ICrudApi<SeguridadRolesModulosViewModel, int> rolesMod)
        {
            this.roles = roles;
            this.modulos = modulos;
            this.rolesMod = rolesMod;
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            try
            {
                var Roles1 = ((ClaimsIdentity)User.Identity).Claims.Where(d => d.Type == "Roles").Select(s1 => s1.Value).FirstOrDefault().Split("|");
                if (string.IsNullOrEmpty(Roles1.Where(a => a == "6").FirstOrDefault()))
                {
                    return RedirectToPage("/NoPermiso");
                }

                Roles = await roles.ObtenerPorId(id);




                return Page();
            }
            catch (ApiException ex)
            {
                Errores error = JsonConvert.DeserializeObject<Errores>(ex.Content.ToString());
                ModelState.AddModelError(string.Empty, error.Message);

                return Page();
            }
        }

        public async Task<IActionResult> OnGetModulosAsync(string id)
        {
            try
            {
                filtro.Codigo1 = Convert.ToInt32(id);
                var ModulosGenerales = await modulos.ObtenerLista("");
                ModulosMios = await rolesMod.ObtenerLista(filtro);
                // Modulos = await rolesMod.ObtenerLista("");
                ModulosMiosS = new SeguridadModulosViewModel[ModulosMios.Length];
                //ModulosS = new SeguridadModulos[Modulos.Length];

                for (int j = 0; j < ModulosMiosS.Length; j++)
                {
                    ModulosMiosS[j] = new SeguridadModulosViewModel();
                }

              

                var i = 0;
                foreach (var item in ModulosMios.Where(a => a.CodModulo != 0))
                {
                    var Modulo = ModulosGenerales.Where(a => a.CodModulo == item.CodModulo).FirstOrDefault();
                    ModulosMiosS[i].CodModulo = Modulo.CodModulo;
                    ModulosMiosS[i].Descripcion = Modulo.Descripcion;

                    i++;

                }


                foreach (var item in ModulosMiosS.Where(a => a.CodModulo != 0))
                {
                    ModulosGenerales = ModulosGenerales.Where(a => a.CodModulo != item.CodModulo).ToArray();
                }



                var resp = new
                {
                    ModulosMiosS,
                    ModulosGenerales
                };



                return new JsonResult(resp);
            }
            catch (ApiException ex)
            {
                Errores error = JsonConvert.DeserializeObject<Errores>(ex.Content.ToString());
                ModelState.AddModelError(string.Empty, error.Message);

                return new JsonResult(error);
            }
        }

        public async Task<IActionResult> OnGetPost([FromBody] string recibidos)
        {
            try
            {
                recibidos = recibidos.Replace("_", " ");
                RecibidoRoles recibido = JsonConvert.DeserializeObject<RecibidoRoles>(recibidos);
                recibido.modulos = recibido.modulos.Replace("ProdCadenaM", "rolesMod");
                VectorRoles rolesModulos1 = JsonConvert.DeserializeObject<VectorRoles>(recibido.modulos);
                SeguridadRolesModulosViewModel[] rolesModulos = new SeguridadRolesModulosViewModel[rolesModulos1.rolesMod.Length];


                short cantidad = 0;
                if (rolesModulos.Length > 0)
                {

                    foreach (var item in rolesModulos1.rolesMod)
                    {

                        rolesModulos[cantidad] = new SeguridadRolesModulosViewModel();
                        rolesModulos[cantidad].CodRol = Convert.ToInt32(recibido.CodRol);
                        rolesModulos[cantidad].CodModulo = item.CodModulo;

                        cantidad++;
                    }
                }
                else
                {
                    rolesModulos = new SeguridadRolesModulosViewModel[1];
                    rolesModulos[0].CodRol = Convert.ToInt32(recibido.CodRol);
                    rolesModulos[0].CodModulo = 0;
                }

                await rolesMod.AgregarBulk(rolesModulos);
                return RedirectToPage("./Index");
            }
            catch (ApiException ex)
            {
                Errores error = JsonConvert.DeserializeObject<Errores>(ex.Content.ToString());
                ModelState.AddModelError(string.Empty, error.Message);

                return new JsonResult(false);
            }
            catch (Exception ex)
            {

                ModelState.AddModelError(string.Empty, ex.Message);

                return new JsonResult(false);
            }
        }
        //Esto es una prueba

        public async Task<IActionResult> OnPostGenerar(string recibidos)
        {
            try
            {
                recibidos = recibidos.Replace("_", " ");
                RecibidoRoles recibido = JsonConvert.DeserializeObject<RecibidoRoles>(recibidos);
                recibido.modulos = recibido.modulos.Replace("ProdCadenaM", "rolesMod");
                VectorRoles rolesModulos1 = JsonConvert.DeserializeObject<VectorRoles>(recibido.modulos);
                SeguridadRolesModulosViewModel[] rolesModulos = new SeguridadRolesModulosViewModel[rolesModulos1.rolesMod.Length];


                short cantidad = 0;
                if (rolesModulos.Length > 0)
                {

                    foreach (var item in rolesModulos1.rolesMod)
                    {

                        rolesModulos[cantidad] = new SeguridadRolesModulosViewModel();
                        rolesModulos[cantidad].CodRol = Convert.ToInt32(recibido.CodRol);
                        rolesModulos[cantidad].CodModulo = item.CodModulo;

                        cantidad++;
                    }
                }
                else
                {
                    rolesModulos = new SeguridadRolesModulosViewModel[1];
                    rolesModulos[0] = new SeguridadRolesModulosViewModel();
                    rolesModulos[0].CodRol = Convert.ToInt32(recibido.CodRol);
                    rolesModulos[0].CodModulo = 0;
                }

                await rolesMod.AgregarBulk(rolesModulos);
                return new JsonResult(true);
            }
            catch (ApiException ex)
            {
                Errores error = JsonConvert.DeserializeObject<Errores>(ex.Content.ToString());
                ModelState.AddModelError(string.Empty, error.Message);

                return new JsonResult(false);
            }
            catch (Exception ex)
            {

                ModelState.AddModelError(string.Empty, ex.Message);

                return new JsonResult(false);
            }
        }
    }
}
