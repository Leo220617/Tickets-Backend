using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using InversionGloblalWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Refit;
using Sicsoft.Checkin.Web.Servicios;
using Sicsoft.CostaRica.Checkin.Web.Models;

namespace Sicsoft.CostaRica.Checkin.Web.Pages.Account
{
    public class RegistroUsuarioModel : PageModel
    {
        private readonly ICrudApi<LoginUsuario, int> service;
        private readonly ICrudApi<UsuariosViewModel, int> users;
        private readonly ICrudApi<RolesViewModel, int> roles;

        [BindProperty]
        public LoginUsuario Input { get; set; }

        [BindProperty]
        public RolesViewModel[] Roles { get; set; }

        [BindProperty]
        public UsuariosViewModel[] Usuarios { get; set; }

        public RegistroUsuarioModel(ICrudApi<LoginUsuario, int> service, ICrudApi<RolesViewModel, int> roles, ICrudApi<UsuariosViewModel, int> users)
        {
            this.service = service;
            this.roles = roles;
            this.users = users;
        }
        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                Roles = await roles.ObtenerLista("");
                 
                
               
            }
            catch (Exception)
            {

                
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            { 

                if (string.IsNullOrEmpty(Input.Clave))
                {
                    throw new Exception("La clave debe contener elementos");
                }

               
                await service.Agregar(Input);
                return Redirect("../Index");
            }
            catch (Exception ex)
            {

                ModelState.AddModelError(string.Empty, ex.Message);
                //return Redirect("/Error");
                return Page();
            }
        }
    }
}
