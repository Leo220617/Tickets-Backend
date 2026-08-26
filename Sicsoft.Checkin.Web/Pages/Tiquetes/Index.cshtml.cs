using System;
using System.Collections.Generic;
using System.IO;
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
using ClosedXML.Excel;
using System.Drawing;

namespace Tickets.Pages.Tiquetes
{
    public class IndexModel : PageModel
    {
        private readonly IConfiguration configuration;
        private readonly ICrudApi<TiquetesViewModel, int> service;
        private readonly ICrudApi<UsuariosViewModel, int> users;
        private readonly ICrudApi<EmpresasViewModel, int> serviceE;
        [BindProperty(SupportsGet = true)]
        public ParametrosFiltros filtro { get; set; }


        [BindProperty]
        public TiquetesViewModel[] Objeto { get; set; }
        [BindProperty]
        public UsuariosViewModel[] Usuarios { get; set; }
        [BindProperty]
        public EmpresasViewModel[] Empresas { get; set; }
        public IndexModel(ICrudApi<TiquetesViewModel, int> service, ICrudApi<UsuariosViewModel, int> users, ICrudApi<EmpresasViewModel, int> serviceE)
        {
            this.service = service;
            this.users = users;
            this.serviceE = serviceE;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var identity = User.Identity as ClaimsIdentity;

                var rolesClaim = identity?.Claims
                    .FirstOrDefault(x => x.Type == "Roles")
                    ?.Value ?? "";

                var roles = rolesClaim.Split(
                    new[] { '|' },
                    StringSplitOptions.RemoveEmptyEntries
                );

                if (!roles.Contains("1"))
                {
                    return RedirectToPage("/NoPermiso");
                }

                if (filtro == null)
                {
                    filtro = new ParametrosFiltros();
                }

                // Determina si acaba de entrar o presionó Buscar.
                var primeraCarga = !Request.Query.Keys.Any(
                    llave => llave.StartsWith(
                        "filtro.",
                        StringComparison.OrdinalIgnoreCase
                    )
                );

                if (primeraCarga)
                {
                    var hoy = DateTime.Today;


                    if (filtro.FechaInicial == DateTime.MinValue)
                    {
                        filtro.FechaInicial = new DateTime(DateTime.Today.Year, 1, 1);
                    }

                    if (filtro.FechaFinal == DateTime.MinValue)
                    {
                        filtro.FechaFinal = DateTime.Today;
                    }

                    // Primera carga: mostrar abiertos.
                    filtro.Texto2 = "AV";

                    // Sincronizar correo solamente al entrar,
                    // no cada vez que se presiona Buscar.
                    await service.RealizarLecturaEmails();
                    await service.LecturaBandejaEntrada();
                }

                // Usuario sin rol 4: solo puede consultar sus tiquetes.
                if (!roles.Contains("4"))
                {
                    var claimUsuario = identity?.Claims
                        .FirstOrDefault(x =>
                            x.Type == ClaimTypes.NameIdentifier
                        )
                        ?.Value;

                    int idUsuario;

                    if (!int.TryParse(claimUsuario, out idUsuario))
                    {
                        return RedirectToPage("/NoPermiso");
                    }

                    filtro.Codigo1 = idUsuario;
                }

                // Ejecutar las consultas simultáneamente.
                var tareaUsuarios = users.ObtenerLista("");
                var tareaEmpresas = serviceE.ObtenerLista("");
                var tareaTiquetes = service.ObtenerLista(filtro);

                await Task.WhenAll(
                    tareaUsuarios,
                    tareaEmpresas,
                    tareaTiquetes
                );

                Usuarios = await tareaUsuarios;
                Empresas = await tareaEmpresas;
                Objeto = await tareaTiquetes;

                return Page();
            }
            catch (ApiException ex)
            {
                var mensaje = ex.Message;

                if (!string.IsNullOrWhiteSpace(ex.Content))
                {
                    try
                    {
                        var error =
                            JsonConvert.DeserializeObject<Errores>(
                                ex.Content
                            );

                        mensaje = error?.Message ?? ex.Content;
                    }
                    catch
                    {
                        mensaje = ex.Content;
                    }
                }

                ModelState.AddModelError(
                    string.Empty,
                    mensaje
                );

                Usuarios = Usuarios ??
                    new UsuariosViewModel[0];

                Empresas = Empresas ??
                    new EmpresasViewModel[0];

                Objeto = Objeto ??
                    new TiquetesViewModel[0];

                return Page();
            }
        }
        public async Task<IActionResult> OnGetExportarFacturacionAsync()
        {
            try
            {
                var identity = User.Identity as ClaimsIdentity;

                var rolesClaim = identity?.Claims
                    .FirstOrDefault(x => x.Type == "Roles")
                    ?.Value ?? "";

                var roles = rolesClaim.Split(
                    new[] { '|' },
                    StringSplitOptions.RemoveEmptyEntries
                );

                if (!roles.Contains("1"))
                {
                    return RedirectToPage("/NoPermiso");
                }

                if (filtro == null)
                {
                    filtro = new ParametrosFiltros();
                }

                var hoy = DateTime.Today;

                // Si no se enviaron fechas, exportar el año actual.
                if (filtro.FechaInicial == DateTime.MinValue)
                {
                    filtro.FechaInicial = new DateTime(hoy.Year, 1, 1);
                }

                if (filtro.FechaFinal == DateTime.MinValue)
                {
                    filtro.FechaFinal = hoy;
                }

                // Usuario sin rol 4: solamente exporta sus tiquetes.
                if (!roles.Contains("4"))
                {
                    var claimUsuario = identity?.Claims
                        .FirstOrDefault(x =>
                            x.Type == ClaimTypes.NameIdentifier
                        )
                        ?.Value;

                    int idUsuario;

                    if (!int.TryParse(claimUsuario, out idUsuario))
                    {
                        return RedirectToPage("/NoPermiso");
                    }

                    filtro.Codigo1 = idUsuario;
                }

                var tareaTiquetes = service.ObtenerLista(filtro);
                var tareaUsuarios = users.ObtenerLista("");
                var tareaEmpresas = serviceE.ObtenerLista("");

                await Task.WhenAll(
                    tareaTiquetes,
                    tareaUsuarios,
                    tareaEmpresas
                );

                var tiquetesReporte =
                    await tareaTiquetes ??
                    new TiquetesViewModel[0];

                var usuariosReporte =
                    await tareaUsuarios ??
                    new UsuariosViewModel[0];

                var empresasReporte =
                    await tareaEmpresas ??
                    new EmpresasViewModel[0];

                using (var libro = new XLWorkbook())
                {
                    var hoja = libro.Worksheets.Add("Facturación");

                    hoja.Cell(1, 1).Value = "REPORTE DE FACTURACIÓN";
                    hoja.Range(1, 1, 1, 8).Merge();
                    hoja.Range(1, 1, 1, 8).Style
                        .Fill.SetBackgroundColor(XLColor.FromHtml("#131E29"));
                    hoja.Range(1, 1, 1, 8).Style
                        .Font.SetFontColor(XLColor.White);
                    hoja.Range(1, 1, 1, 8).Style
                        .Font.SetBold();
                    hoja.Range(1, 1, 1, 8).Style
                        .Font.SetFontSize(16);
                    hoja.Range(1, 1, 1, 8).Style
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                    hoja.Cell(2, 1).Value =
                        "Periodo: " +
                        filtro.FechaInicial.ToString("dd/MM/yyyy") +
                        " al " +
                        filtro.FechaFinal.ToString("dd/MM/yyyy");

                    hoja.Range(2, 1, 2, 8).Merge();
                    hoja.Range(2, 1, 2, 8).Style
                        .Font.SetFontColor(XLColor.FromHtml("#5F6B7A"));
                    hoja.Range(2, 1, 2, 8).Style
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                    var filaEncabezado = 4;

                    hoja.Cell(filaEncabezado, 1).Value = "Fecha";
                    hoja.Cell(filaEncabezado, 2).Value = "Empresa";
                    hoja.Cell(filaEncabezado, 3).Value = "Consultor";
                    hoja.Cell(filaEncabezado, 4).Value = "Horas";
                    hoja.Cell(filaEncabezado, 5).Value = "Tiquete";
                    hoja.Cell(filaEncabezado, 6).Value = "Asunto";
                    hoja.Cell(filaEncabezado, 7).Value = "Solicitante";
                    hoja.Cell(filaEncabezado, 8).Value = "Tipo";

                    var fila = filaEncabezado + 1;

                    foreach (var ticket in tiquetesReporte
                        .OrderBy(x => x.FechaTicket)
                        .ThenBy(x => x.id))
                    {
                        var usuario = usuariosReporte
                            .FirstOrDefault(x =>
                                x.id == ticket.idLoginAsignado
                            );

                        var empresa = empresasReporte
                            .FirstOrDefault(x =>
                                x.id == ticket.idEmpresa
                            );

                        if (ticket.FechaTicket.HasValue)
                        {
                            hoja.Cell(fila, 1).Value =
                                ticket.FechaTicket.Value.Date;

                            hoja.Cell(fila, 1).Style.DateFormat.Format =
                                "dd/MM/yyyy";
                        }
                        else
                        {
                            hoja.Cell(fila, 1).Value = "";
                        }

                        hoja.Cell(fila, 2).Value =
                            empresa?.Nombre ?? "Sin empresa";

                        hoja.Cell(fila, 3).Value =
                            usuario?.Nombre ?? "Sin asignar";

                        hoja.Cell(fila, 4).Value =
                            ConvertirHorasExcel(ticket.Duracion);

                        hoja.Cell(fila, 4).Style.NumberFormat.Format =
                            "0.00";

                        hoja.Cell(fila, 5).Value = ticket.id;
                        hoja.Cell(fila, 6).Value = ticket.Asunto ?? "";
                        hoja.Cell(fila, 7).Value =
                            ticket.PersonaTicket ?? "";

                        hoja.Cell(fila, 8).Value =
                            NombreTipoExcel(ticket.Tipo);

                        fila++;
                    }

                    var ultimaFilaDatos = fila - 1;

                    var rangoEncabezado = hoja.Range(
                        filaEncabezado,
                        1,
                        filaEncabezado,
                        8
                    );

                    rangoEncabezado.Style
                        .Fill.SetBackgroundColor(XLColor.FromHtml("#0073BB"));
                    rangoEncabezado.Style
                        .Font.SetFontColor(XLColor.White);
                    rangoEncabezado.Style
                        .Font.SetBold();
                    rangoEncabezado.Style
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                    if (ultimaFilaDatos >= filaEncabezado + 1)
                    {
                        var tabla = hoja.Range(
                            filaEncabezado,
                            1,
                            ultimaFilaDatos,
                            8
                        ).CreateTable("TablaFacturacion");

                        tabla.Theme = XLTableTheme.TableStyleMedium2;
                        tabla.ShowAutoFilter = true;

                        var filaTotal = ultimaFilaDatos + 2;

                        hoja.Cell(filaTotal, 3).Value = "TOTAL HORAS:";
                        hoja.Cell(filaTotal, 3).Style.Font.SetBold();

                        hoja.Cell(filaTotal, 4).FormulaA1 =
                            "SUM(D" +
                            (filaEncabezado + 1) +
                            ":D" +
                            ultimaFilaDatos +
                            ")";

                        hoja.Cell(filaTotal, 4).Style.Font.SetBold();
                        hoja.Cell(filaTotal, 4).Style.NumberFormat.Format =
                            "0.00";
                    }
                    else
                    {
                        hoja.Cell(fila, 1).Value =
                            "No se encontraron registros para los filtros seleccionados.";

                        hoja.Range(fila, 1, fila, 8).Merge();
                        hoja.Range(fila, 1, fila, 8).Style
                            .Font.SetFontColor(XLColor.FromHtml("#5F6B7A"));
                        hoja.Range(fila, 1, fila, 8).Style
                            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                    }

                    hoja.SheetView.FreezeRows(filaEncabezado);
                    hoja.Column(1).Width = 13;
                    hoja.Column(2).Width = 25;
                    hoja.Column(3).Width = 22;
                    hoja.Column(4).Width = 12;
                    hoja.Column(5).Width = 12;
                    hoja.Column(6).Width = 70;
                    hoja.Column(7).Width = 25;
                    hoja.Column(8).Width = 18;

                    hoja.Column(6).Style.Alignment.WrapText = true;
                    hoja.RangeUsed().Style.Alignment
                        .SetVertical(XLAlignmentVerticalValues.Center);

                    using (var memoria = new MemoryStream())
                    {
                        libro.SaveAs(memoria);

                        var nombreArchivo =
                            "Facturacion_" +
                            filtro.FechaInicial.ToString("yyyyMMdd") +
                            "_" +
                            filtro.FechaFinal.ToString("yyyyMMdd") +
                            ".xlsx";

                        return File(
                            memoria.ToArray(),
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                            nombreArchivo
                        );
                    }
                }
            }
            catch (ApiException ex)
            {
                var mensaje = ex.Message;

                if (!string.IsNullOrWhiteSpace(ex.Content))
                {
                    mensaje = ex.Content;
                }

                ModelState.AddModelError(string.Empty, mensaje);

                // Recarga la página conservando los filtros.
                return await OnGetAsync();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "No fue posible generar el Excel: " + ex.Message
                );

                return await OnGetAsync();
            }
        }

        private static decimal ConvertirHorasExcel(string duracion)
        {
            if (string.IsNullOrWhiteSpace(duracion))
            {
                return 0;
            }

            var partes = duracion.Trim().Split(':');

            int horas;
            int minutos;

            if (partes.Length >= 2 &&
                int.TryParse(partes[0], out horas) &&
                int.TryParse(partes[1], out minutos))
            {
                return Math.Round(
                    horas + (minutos / 60m),
                    2
                );
            }

            return 0;
        }

        private static string NombreTipoExcel(string tipo)
        {
            switch ((tipo ?? "").Trim().ToUpperInvariant())
            {
                case "F":
                    return "FACTURACIÓN";

                case "S":
                    return "SAP";

                case "D":
                    return "DESARROLLO";

                default:
                    return "SIN DEFINIR";
            }
        }

        public async Task<IActionResult> OnGetEliminar(int id)
        {
            try
            {


                await service.EliminarUsuario(id);

                return new JsonResult(true);
            }
            catch (ApiException ex)
            {
                return new JsonResult(false);
            }
        }

        public async Task<IActionResult> OnPostUnificarAsync(
    int ticketPrincipalId,
    int ticketSecundarioId)
        {
            try
            {
                var identity = User.Identity as ClaimsIdentity;

                var rolesClaim = identity?.Claims
                    .FirstOrDefault(x => x.Type == "Roles")
                    ?.Value ?? "";

                var roles = rolesClaim.Split(
                    new[] { '|' },
                    StringSplitOptions.RemoveEmptyEntries
                );

                if (!roles.Contains("2"))
                {
                    return new JsonResult(new
                    {
                        correcto = false,
                        mensaje = "No tiene permiso para unificar tiquetes."
                    });
                }

                if (ticketPrincipalId <= 0 ||
                    ticketSecundarioId <= 0)
                {
                    return new JsonResult(new
                    {
                        correcto = false,
                        mensaje = "Los números de tiquete no son válidos."
                    });
                }

                if (ticketPrincipalId == ticketSecundarioId)
                {
                    return new JsonResult(new
                    {
                        correcto = false,
                        mensaje = "No puede unificar un tiquete consigo mismo."
                    });
                }

                var solicitud = new UnificarTiquetesRequest
                {
                    TicketPrincipalId = ticketPrincipalId,
                    TicketSecundarioId = ticketSecundarioId
                };

                var respuesta = await service.UnificarTiquetes(
                    solicitud
                );

                if (!respuesta.IsSuccessStatusCode)
                {
                    var contenidoError =
                        await respuesta.Content.ReadAsStringAsync();

                    return new JsonResult(new
                    {
                        correcto = false,
                        mensaje = string.IsNullOrWhiteSpace(contenidoError)
                            ? "No fue posible unificar los tiquetes."
                            : contenidoError.Trim('"')
                    });
                }

                return new JsonResult(new
                {
                    correcto = true,
                    mensaje = "Los tiquetes fueron unificados correctamente."
                });
            }
            catch (ApiException ex)
            {
                var mensaje = string.IsNullOrWhiteSpace(ex.Content)
                    ? ex.Message
                    : ex.Content.Trim('"');

                return new JsonResult(new
                {
                    correcto = false,
                    mensaje = mensaje
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new
                {
                    correcto = false,
                    mensaje = "No fue posible unificar los tiquetes: " +
                              ex.Message
                });
            }
        }
    }
}

