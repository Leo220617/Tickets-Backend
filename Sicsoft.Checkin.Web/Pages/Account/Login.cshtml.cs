using System;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Threading.Tasks;
using InversionGloblalWeb.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using Refit;
using Sicsoft.Checkin.Web.Models;
using Sicsoft.Checkin.Web.Servicios;
using Sicsoft.CostaRica.Checkin.Web.Models;

namespace Sicsoft.Checkin.Web
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly ICrudApi<LoginDevolucion,int> checkInService;

        [BindProperty]
        public LoginViewModel Input { get; set; }


        public LoginModel(ICrudApi<LoginDevolucion, int> checkInService)
        {
            this.checkInService = checkInService;
        }
        public void OnGet()
        {

        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await HttpContext.SignOutAsync();
                return Page();
            }
            ActionResult response = Page();
            try
            {
                var resultado = await checkInService.Login(Input.Email, Input.Password);
                string str = "";

                foreach(var item in resultado.Seguridad)
                {
                    str += item.CodModulo + "|";
                }


                var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, resultado.id.ToString()));
                identity.AddClaim(new Claim(ClaimTypes.Name, resultado.Email));
                identity.AddClaim(new Claim(ClaimTypes.UserData, resultado.token));
              
                identity.AddClaim(new Claim(ClaimTypes.Role, resultado.idRol.ToString()));
                identity.AddClaim(new Claim("Roles",str));
               
                var principal = new ClaimsPrincipal(identity);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                

                return RedirectToPage("/Index");

            }
            catch (ValidationApiException)
            {
                // handle validation here by using validationException.Content,
                // which is type of ProblemDetails according to RFC 7807

                // If the response contains additional properties on the problem details,
                // they will be added to the validationException.Content.Extensions collection.
            }
            catch (ApiException exception)
            {
                if(exception.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                {
                    Errores error = JsonConvert.DeserializeObject<Errores>(exception.Content.ToString());
                    ModelState.AddModelError("Email", error.Message);
                    return Page();
                }
               
            }
           catch(Exception ex)
            {
                ModelState.AddModelError("Email", ex.Message);
                return Page();
            }
           
            return response;


        }
    }
}