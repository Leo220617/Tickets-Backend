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
using Tickets.Models;

namespace Tickets.Pages.Tiquetes
{
    public class EditarModel : PageModel
    {
        private readonly ICrudApi<TiquetesViewModel, int> service;
        private readonly ICrudApi<UsuariosViewModel, int> users;
        private readonly ICrudApi<EmpresasViewModel, int> serviceE;
        private readonly ICrudApi<Adjuntos, int> serviceAdj;
        private readonly ICrudApi<RespuestasViewModel, int> respuestas;

        [BindProperty] public TiquetesViewModel Tiquete { get; set; }
        [BindProperty] public UsuariosViewModel[] Usuarios { get; set; }
        [BindProperty] public EmpresasViewModel[] Empresas { get; set; }
        [BindProperty] public Adjuntos[] Adj { get; set; }
        [BindProperty] public RespuestasViewModel[] Respuestas { get; set; }

        [BindProperty]
        public string NuevaRespuesta { get; set; }
        public EditarModel(
            ICrudApi<TiquetesViewModel, int> service,
            ICrudApi<UsuariosViewModel, int> users,
            ICrudApi<EmpresasViewModel, int> serviceE,
            ICrudApi<Adjuntos, int> serviceAdj,
            ICrudApi<RespuestasViewModel, int> respuestas)
        {
            this.service = service;
            this.users = users;
            this.serviceE = serviceE;
            this.serviceAdj = serviceAdj;
            this.respuestas = respuestas;
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            try
            {
                var rolesClaim = ((ClaimsIdentity)User.Identity).Claims
                    .FirstOrDefault(c => c.Type == "Roles")?.Value ?? "";

                if (!rolesClaim.Split('|').Contains("1"))
                    return RedirectToPage("/NoPermiso");

                if (id != 0)
                {
                    // Primero revisar si el cliente respondió.
                    await service.LeerRespuestasTicket(id);

                    // Después consultar las respuestas actualizadas.
                    var filtro = new ParametrosFiltros
                    {
                        Codigo1 = id
                    };

                    Adj = await serviceAdj.ObtenerLista(filtro);
                    Respuestas = await respuestas.ObtenerLista(filtro);
                }

                Empresas = await serviceE.ObtenerLista("");
                Usuarios = await users.ObtenerLista("");
                Tiquete = await service.ObtenerPorId(id);

                if (id != 0 && Adj != null)
                {
                    foreach (var item in Adj)
                        Tiquete.Adjunto += item.Adjunto + "¶";
                }

                Tiquete.DuracionReal = Tiquete.Duracion;
                return Page();
            }
            catch (ApiException ex)
            {
                ModelState.AddModelError(string.Empty, ObtenerError(ex));
                return Page();
            }
        }

        // Guarda los datos generales sin recargar la página.
        public async Task<IActionResult> OnPostGuardarAsync()
        {
            try
            {
                if (Tiquete == null || string.IsNullOrWhiteSpace(Tiquete.Tipo))
                    return BadRequest(new { ok = false, mensaje = "Debe seleccionar el tipo del ticket antes de guardar." });

                await GuardarTicketAsync();
                return new JsonResult(new { ok = true, mensaje = "Los cambios se guardaron correctamente." });
            }
            catch (ApiException ex)
            {
                Response.StatusCode = 400;
                return new JsonResult(new { ok = false, mensaje = ObtenerError(ex) });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return new JsonResult(new { ok = false, mensaje = ex.Message });
            }
        }

        // Autoguardado de agente, empresa y tipo. No vuelve a insertar adjuntos.
        public async Task<IActionResult> OnPostGuardarCamposAsync()
        {
            try
            {
                if (Tiquete == null || string.IsNullOrWhiteSpace(Tiquete.Tipo))
                    return BadRequest(new { ok = false, mensaje = "Debe seleccionar el tipo del ticket antes de guardar." });

                await GuardarTicketAsync(false);
                return new JsonResult(new { ok = true, mensaje = "Cambios guardados automáticamente." });
            }
            catch (ApiException ex)
            {
                Response.StatusCode = 400;
                return new JsonResult(new { ok = false, mensaje = ObtenerError(ex) });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return new JsonResult(new { ok = false, mensaje = ex.Message });
            }
        }

        // Guarda una respuesta o nota. Solo las respuestas se envían por correo.
        public async Task<IActionResult> OnPostResponderAsync(
      int idTicket,
      string texto,
      bool esNotaInterna,
      string nuevoStatus)
        {
            try
            {
                if (idTicket <= 0)
                    return BadRequest(new { ok = false, mensaje = "El ticket no es válido." });

                if (Tiquete == null || string.IsNullOrWhiteSpace(Tiquete.Tipo))
                    return BadRequest(new { ok = false, mensaje = "Debe seleccionar el tipo del ticket antes de guardar una respuesta o nota interna." });

                if (string.IsNullOrWhiteSpace(texto))
                {
                    return BadRequest(new
                    {
                        ok = false,
                        mensaje = "Escriba un mensaje antes de continuar."
                    });
                }

                if (!esNotaInterna)
                {
                    if (nuevoStatus != "V" && nuevoStatus != "C")
                    {
                        return BadRequest(new
                        {
                            ok = false,
                            mensaje = "Debe seleccionar Validación o Cerrado."
                        });
                    }

                    Tiquete.Status = nuevoStatus;
                }

                var claimId = ((ClaimsIdentity)User.Identity).Claims
                    .FirstOrDefault(c =>
                        c.Type == ClaimTypes.NameIdentifier
                    )?.Value;
                if (!int.TryParse(claimId, out var idUsuario))
                    return Unauthorized();

                var nuevaRespuesta = new RespuestasViewModel
                {
                    idTicket = idTicket,
                    idUsuario = idUsuario,
                    Respuesta = texto.Trim(),
                    EsNotaInterna = esNotaInterna
                };

                // Persiste también el tipo seleccionado antes de registrar la conversación.
                await GuardarTicketAsync();

                var guardada = await respuestas.Agregar(nuevaRespuesta);

                if (!esNotaInterna)
                    await respuestas.ReenvioCorreo(guardada.id);

                var nombre = User.Identity?.Name ?? (esNotaInterna ? "Equipo de soporte" : "Soporte");

                return new JsonResult(new
                {
                    ok = true,
                    mensaje = esNotaInterna
                        ? "La nota interna se guardó correctamente."
                        : "La respuesta se envió correctamente.",
                    respuesta = new
                    {
                        id = guardada.id,
                        texto = guardada.Respuesta ?? nuevaRespuesta.Respuesta,
                        esNotaInterna,
                        autor = nombre,
                        fecha = DateTime.Now.ToString("dd/MM/yyyy HH:mm")
                    }
                });
            }
            catch (ApiException ex)
            {
                Response.StatusCode = 400;
                return new JsonResult(new { ok = false, mensaje = ObtenerError(ex) });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return new JsonResult(new { ok = false, mensaje = ex.Message });
            }
        }

        private async Task GuardarTicketAsync(bool guardarAdjuntos = true)
        {
            if (guardarAdjuntos && !string.IsNullOrWhiteSpace(Tiquete.Adjunto))
            {
                var adjuntos = Tiquete.Adjunto.Split('¶')
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => new Adjuntos { idTicket = Tiquete.id, Adjunto = x })
                    .ToArray();

                if (adjuntos.Length > 0)
                    await serviceAdj.AgregarBulk(adjuntos);
            }

            if (Tiquete.Duracion == "00:00:00" ||
                (Tiquete.Duracion != Tiquete.DuracionReal &&
                 Convert.ToDateTime(Tiquete.DuracionReal) > Convert.ToDateTime(Tiquete.Duracion)))
            {
                Tiquete.Duracion = Tiquete.DuracionReal;
            }

            await service.Editar(Tiquete);
        }

        private static string ObtenerError(ApiException ex)
        {
            return string.IsNullOrWhiteSpace(ex.Content) ? ex.Message : ex.Content;
        }
    }
}
