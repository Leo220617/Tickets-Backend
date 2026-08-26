using InversionGloblalWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using Refit;
using Sicsoft.Checkin.Web.Servicios;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Tickets.Models;

namespace Tickets.Pages.Planificacion
{
    public class ActividadIdDTO
    {
        public int id { get; set; }
    }

    public class ActualizarFechaDTO
    {
        public int id { get; set; }

        public DateTime fecha { get; set; }
    }

    public class CalendarioModel : PageModel
    {
        private readonly ICrudApi<ActividadesViewModel, int> service;
        private readonly ICrudApi<EmpresasViewModel, int> serviceClientes;
        private readonly ICrudApi<TipoActividadesViewModel, int> serviceTA;
        private readonly ICrudApi<UsuariosViewModel, int> serviceUsuarios;

        [BindProperty]
        public ActividadesViewModel[] Calendario { get; set; }

        [BindProperty]
        public EmpresasViewModel[] ObjetoClientes { get; set; }

        [BindProperty]
        public TipoActividadesViewModel[] ObjetoTA { get; set; }

        [BindProperty]
        public UsuariosViewModel[] Usuarios { get; set; }

        public CalendarioModel(
            ICrudApi<ActividadesViewModel, int> service,
            ICrudApi<EmpresasViewModel, int> serviceClientes,
            ICrudApi<TipoActividadesViewModel, int> serviceTA,
            ICrudApi<UsuariosViewModel, int> serviceUsuarios)
        {
            this.service = service;
            this.serviceClientes = serviceClientes;
            this.serviceTA = serviceTA;
            this.serviceUsuarios = serviceUsuarios;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            Calendario = Array.Empty<ActividadesViewModel>();
            ObjetoClientes = Array.Empty<EmpresasViewModel>();
            ObjetoTA = Array.Empty<TipoActividadesViewModel>();
            Usuarios = Array.Empty<UsuariosViewModel>();

            try
            {
                var identity = User.Identity as ClaimsIdentity;

                var rolesClaim = identity?.Claims
                    .FirstOrDefault(c => c.Type == "Roles")
                    ?.Value ?? "";

                var roles = rolesClaim.Split(
                    new[] { '|' },
                    StringSplitOptions.RemoveEmptyEntries
                );

                if (!roles.Contains("20"))
                {
                    return RedirectToPage("/NoPermiso");
                }

                var inicioAnio = new DateTime(
                    DateTime.Today.Year,
                    1,
                    1
                );

                var finAnio = new DateTime(
                    DateTime.Today.Year,
                    12,
                    31
                );

                var filtroCalendario = new ParametrosFiltros
                {
                    FechaInicial = inicioAnio,
                    FechaFinal = finAnio
                };

                var filtroEmpresas =
                    new ParametrosFiltros();

                var tareaCalendario =
                    service.ObtenerLista(filtroCalendario);

                var tareaEmpresas =
                    serviceClientes.ObtenerLista(filtroEmpresas);

                var tareaTipos =
                    serviceTA.ObtenerLista("");

                var tareaUsuarios =
                    serviceUsuarios.ObtenerLista("");

                await Task.WhenAll(
                    tareaCalendario,
                    tareaEmpresas,
                    tareaTipos,
                    tareaUsuarios
                );

                Calendario =
                    await tareaCalendario ??
                    Array.Empty<ActividadesViewModel>();

                ObjetoClientes =
                    await tareaEmpresas ??
                    Array.Empty<EmpresasViewModel>();

                ObjetoTA =
                    await tareaTipos ??
                    Array.Empty<TipoActividadesViewModel>();

                Usuarios =
                    await tareaUsuarios ??
                    Array.Empty<UsuariosViewModel>();
            }
            catch (ApiException ex)
            {
                ModelState.AddModelError(
                    string.Empty,
                    ObtenerError(ex)
                );
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "No fue posible cargar el calendario: " +
                    ex.Message
                );
            }

            return Page();
        }

        public async Task<IActionResult> OnPostCrearAsync(
            [FromBody] ActividadesViewModel actividad)
        {
            try
            {
                if (actividad == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        mensaje = "No se recibió la actividad."
                    });
                }

                if (actividad.idUsuario <= 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        mensaje = "Debe seleccionar el usuario responsable."
                    });
                }

                if (actividad.idTipoActividad <= 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        mensaje = "Debe seleccionar el tipo de actividad."
                    });
                }

                if (actividad.fechaAgendada == DateTime.MinValue)
                {
                    return BadRequest(new
                    {
                        success = false,
                        mensaje = "Debe seleccionar la fecha."
                    });
                }

                if (string.IsNullOrWhiteSpace(actividad.titulo))
                {
                    return BadRequest(new
                    {
                        success = false,
                        mensaje = "Debe escribir el título de la actividad."
                    });
                }

                actividad.titulo = actividad.titulo.Trim();
                actividad.comentario =
                    actividad.comentario?.Trim() ?? "";

                actividad.estado = "Pendiente";

                var resultado =
                    await service.Agregar(actividad);

                return new JsonResult(new
                {
                    success = true,
                    mensaje = "La actividad fue asignada correctamente.",
                    actividad = resultado
                });
            }
            catch (ApiException ex)
            {
                Response.StatusCode = 400;

                return new JsonResult(new
                {
                    success = false,
                    mensaje = ObtenerError(ex)
                });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;

                return new JsonResult(new
                {
                    success = false,
                    mensaje = ex.Message
                });
            }
        }

        public async Task<IActionResult> OnPostMarcarRealizadoAsync(
            [FromBody] ActividadIdDTO datos)
        {
            try
            {
                if (datos == null || datos.id <= 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        mensaje = "La actividad no es válida."
                    });
                }

                var actividad = new ActividadesViewModel
                {
                    id = datos.id,
                    estado = "Realizado"
                };

                await service.Editar(actividad);

                return new JsonResult(new
                {
                    success = true,
                    mensaje = "La actividad fue marcada como realizada."
                });
            }
            catch (ApiException ex)
            {
                Response.StatusCode = 400;

                return new JsonResult(new
                {
                    success = false,
                    mensaje = ObtenerError(ex)
                });
            }
        }

        public async Task<IActionResult> OnPostActualizarFechaAsync(
            [FromBody] ActualizarFechaDTO datos)
        {
            try
            {
                if (datos == null ||
                    datos.id <= 0 ||
                    datos.fecha == DateTime.MinValue)
                {
                    return BadRequest(new
                    {
                        success = false,
                        mensaje = "La actividad o la fecha no son válidas."
                    });
                }

                var actividad = new ActividadesViewModel
                {
                    id = datos.id,
                    fechaAgendada = datos.fecha
                };

                await service.Editar(actividad);

                return new JsonResult(new
                {
                    success = true,
                    mensaje = "La actividad fue reprogramada."
                });
            }
            catch (ApiException ex)
            {
                Response.StatusCode = 400;

                return new JsonResult(new
                {
                    success = false,
                    mensaje = ObtenerError(ex)
                });
            }
        }

        private static string ObtenerError(
            ApiException ex)
        {
            if (string.IsNullOrWhiteSpace(ex.Content))
            {
                return ex.Message;
            }

            try
            {
                var error =
                    JsonConvert.DeserializeObject<Errores>(
                        ex.Content
                    );

                return error?.Message ?? ex.Content;
            }
            catch
            {
                return ex.Content.Trim('"');
            }
        }
    }
}