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

        [BindProperty]
        public int TiempoHoras { get; set; }

        [BindProperty]
        public int TiempoMinutos { get; set; }

        [BindProperty]
        public int EstimadoHoras { get; set; }

        [BindProperty]
        public int EstimadoMinutos { get; set; }
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
                return new JsonResult(new
                {
                    ok = true,
                    mensaje = "Los cambios se guardaron correctamente.",
                    duracion = Tiquete.Duracion,
                    duracionEstimada = Tiquete.DuracionEstimada
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

        // Autoguardado de agente, empresa y tipo. No vuelve a insertar adjuntos.
        public async Task<IActionResult> OnPostGuardarCamposAsync()
        {
            try
            {
                if (Tiquete == null || string.IsNullOrWhiteSpace(Tiquete.Tipo))
                    return BadRequest(new { ok = false, mensaje = "Debe seleccionar el tipo del ticket antes de guardar." });

                await GuardarTicketAsync(
         guardarAdjuntos: false,
         aplicarTiempo: false
     );
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
      string nuevoStatus,
      string destinatarios)
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
                if (!esNotaInterna && string.IsNullOrWhiteSpace(destinatarios))
                {
                    return BadRequest(new
                    {
                        ok = false,
                        mensaje = "Debe ingresar al menos un destinatario."
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
                    await respuestas.ReenvioCorreo( guardada.id, destinatarios );

                var nombre = User.Identity?.Name ?? (esNotaInterna ? "Equipo de soporte" : "Soporte");

                return new JsonResult(new
                {
                    ok = true,
                    mensaje = esNotaInterna
             ? "La nota interna se guardó correctamente."
             : "La respuesta se envió correctamente.",

                    duracion = Tiquete.Duracion,
                    duracionEstimada = Tiquete.DuracionEstimada,

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

        private async Task GuardarTicketAsync(
            bool guardarAdjuntos = true,
            bool aplicarTiempo = true)
        {
            if (guardarAdjuntos &&
        !string.IsNullOrWhiteSpace(Tiquete.Adjunto))
            {
                var filtroAdjuntos = new ParametrosFiltros
                {
                    Codigo1 = Tiquete.id
                };

                // Consultar los adjuntos que ya están guardados.
                var adjuntosExistentes =
                    await serviceAdj.ObtenerLista(filtroAdjuntos)
                    ?? Array.Empty<Adjuntos>();

                var contenidosExistentes = new HashSet<string>(
                    adjuntosExistentes
                        .Where(x =>
                            !string.IsNullOrWhiteSpace(x.Adjunto)
                        )
                        .Select(x => x.Adjunto),
                    StringComparer.Ordinal
                );

                // Insertar solamente archivos que todavía no existan.
                var adjuntosNuevos = Tiquete.Adjunto
                    .Split('¶')
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x)
                    )
                    .Distinct()
                    .Where(x =>
                        !contenidosExistentes.Contains(x)
                    )
                    .Select(x => new Adjuntos
                    {
                        idTicket = Tiquete.id,
                        Adjunto = x
                    })
                    .ToArray();

                if (adjuntosNuevos.Length > 0)
                {
                    await serviceAdj.AgregarBulk(
                        adjuntosNuevos
                    );
                }
            }

            if (aplicarTiempo)
            {
                if (TiempoHoras < 0)
                {
                    throw new Exception(
                        "Las horas invertidas no pueden ser negativas."
                    );
                }

                if (TiempoMinutos < 0 || TiempoMinutos > 59)
                {
                    throw new Exception(
                        "Los minutos invertidos deben estar entre 0 y 59."
                    );
                }

                if (EstimadoHoras < 0)
                {
                    throw new Exception(
                        "Las horas estimadas no pueden ser negativas."
                    );
                }

                if (EstimadoMinutos < 0 || EstimadoMinutos > 59)
                {
                    throw new Exception(
                        "Los minutos estimados deben estar entre 0 y 59."
                    );
                }

                TimeSpan tiempoActual;

                if (!TimeSpan.TryParse(
                    Tiquete.Duracion ?? "00:00:00",
                    out tiempoActual))
                {
                    tiempoActual = TimeSpan.Zero;
                }

                var tiempoAgregado =
                    TimeSpan.FromHours(TiempoHoras) +
                    TimeSpan.FromMinutes(TiempoMinutos);

                var tiempoTotal = tiempoActual + tiempoAgregado;

                Tiquete.Duracion = FormatearTiempo(tiempoTotal);
                Tiquete.DuracionReal = Tiquete.Duracion;

                var tiempoEstimado =
                    TimeSpan.FromHours(EstimadoHoras) +
                    TimeSpan.FromMinutes(EstimadoMinutos);

                Tiquete.DuracionEstimada =
                    FormatearTiempo(tiempoEstimado);
            }

            await service.Editar(Tiquete);
        }
        private static string FormatearTiempo(TimeSpan tiempo)
        {
            var horasTotales = (int)Math.Floor(tiempo.TotalHours);

            return horasTotales.ToString("00") +
                   ":" +
                   tiempo.Minutes.ToString("00") +
                   ":00";
        }
        private static string ObtenerError(ApiException ex)
        {
            return string.IsNullOrWhiteSpace(ex.Content) ? ex.Message : ex.Content;
        }
        public async Task<IActionResult> OnPostUnificarAsync(
    int ticketPrincipalId,
    int ticketSecundarioId)
        {
            try
            {
                var rolesClaim = ((ClaimsIdentity)User.Identity)
                    .Claims
                    .FirstOrDefault(c => c.Type == "Roles")
                    ?.Value ?? "";

                var roles = rolesClaim.Split(
                    new[] { '|' },
                    StringSplitOptions.RemoveEmptyEntries
                );

                if (!roles.Contains("2"))
                {
                    return new JsonResult(new
                    {
                        ok = false,
                        mensaje = "No tiene permiso para unificar tiquetes."
                    });
                }

                if (ticketPrincipalId <= 0 ||
                    ticketSecundarioId <= 0)
                {
                    return new JsonResult(new
                    {
                        ok = false,
                        mensaje = "Los números de tiquete no son válidos."
                    });
                }

                if (ticketPrincipalId == ticketSecundarioId)
                {
                    return new JsonResult(new
                    {
                        ok = false,
                        mensaje = "No puede unificar el tiquete consigo mismo."
                    });
                }

                var solicitud = new UnificarTiquetesRequest
                {
                    TicketPrincipalId = ticketPrincipalId,
                    TicketSecundarioId = ticketSecundarioId
                };

                var respuestaApi = await service.UnificarTiquetes(
                    solicitud
                );

                var contenido = respuestaApi.Content == null
                    ? ""
                    : await respuestaApi.Content.ReadAsStringAsync();

                if (!respuestaApi.IsSuccessStatusCode)
                {
                    return new JsonResult(new
                    {
                        ok = false,
                        mensaje = string.IsNullOrWhiteSpace(contenido)
                            ? "No fue posible unificar los tiquetes."
                            : contenido.Trim('"')
                    });
                }

                return new JsonResult(new
                {
                    ok = true,
                    mensaje = "Los tiquetes fueron unificados correctamente.",
                    ticketPrincipalId = ticketPrincipalId
                });
            }
            catch (ApiException ex)
            {
                return new JsonResult(new
                {
                    ok = false,
                    mensaje = string.IsNullOrWhiteSpace(ex.Content)
                        ? ex.Message
                        : ex.Content.Trim('"')
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new
                {
                    ok = false,
                    mensaje = "No fue posible unificar los tiquetes: " +
                              ex.Message
                });
            }
        }
    }
}
