using InversionGloblalWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using Refit;
using Sicsoft.Checkin.Web.Servicios;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Tickets.Models;

namespace Tickets.Pages.Planificacion
{
    public class MarcarRealizadoDTO
    {
        public int id { get; set; }
    }
    public class CalendarioModel : PageModel
    {
        private readonly ICrudApi<ActividadesViewModel, int> service;
        private readonly ICrudApi<EmpresasViewModel, int> serviceClientes;
        private readonly ICrudApi<TipoActividadesViewModel, int> serviceTA;




        [BindProperty]
        public ActividadesViewModel[] Calendario { get; set; }
        [BindProperty]
        public EmpresasViewModel[] ObjetoClientes { get; set; }

        [BindProperty]
        public TipoActividadesViewModel[] ObjetoTA { get; set; }

        public CalendarioModel(ICrudApi<ActividadesViewModel, int> service, ICrudApi<EmpresasViewModel, int> serviceClientes, ICrudApi<TipoActividadesViewModel, int> serviceTA)
        {
            this.service = service;
            this.serviceClientes = serviceClientes;
            this.serviceTA = serviceTA;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var Roles1 = ((ClaimsIdentity)User.Identity).Claims.Where(d => d.Type == "Roles").Select(s1 => s1.Value).FirstOrDefault().Split("|");

                ParametrosFiltros filtroRuta = new ParametrosFiltros();
                if (string.IsNullOrEmpty(Roles1.Where(a => a == "20").FirstOrDefault()))
                {
                    return RedirectToPage("/NoPermiso");
                }
                var inicioAnio = new DateTime(DateTime.Now.Year, 1, 1);
                var finAnio = inicioAnio.AddYears(1);
                filtroRuta.FechaInicial = inicioAnio;
                filtroRuta.FechaFinal = finAnio;

                Calendario = await service.ObtenerLista(filtroRuta);

                ParametrosFiltros filtroCliente = new ParametrosFiltros();
                ObjetoClientes = await serviceClientes.ObtenerLista(filtroCliente);

                ObjetoTA = await serviceTA.ObtenerLista("");

            }
            catch (ApiException ex)
            {


            }
            catch (Exception ex)
            {


            }
            return Page();
        }

        public async Task<IActionResult> OnPostPlanificador(string recibidos)
        {
            try
            {
                ActividadesViewModel recibido = new ActividadesViewModel();
                var ms = new MemoryStream();
                await Request.Body.CopyToAsync(ms);

                byte[] compressedData = ms.ToArray();

                // Descomprimir los datos utilizando GZip
                using (var compressedStream = new MemoryStream(compressedData))
                using (var decompressedStream = new MemoryStream())
                {
                    using (var decompressionStream = new GZipStream(compressedStream, CompressionMode.Decompress))
                    {
                        decompressionStream.CopyTo(decompressedStream);
                    }

                    // Convertir los datos descomprimidos a una cadena JSON
                    var jsonString = System.Text.Encoding.UTF8.GetString(decompressedStream.ToArray());

                    // Procesar la cadena JSON como desees
                    // Por ejemplo, puedes deserializarla a un objeto C# utilizando Newtonsoft.Json
                    recibido = Newtonsoft.Json.JsonConvert.DeserializeObject<ActividadesViewModel>(jsonString);
                }

                recibido.idUsuario = Convert.ToInt32(((ClaimsIdentity)User.Identity).Claims.Where(d => d.Type == ClaimTypes.Actor).Select(s1 => s1.Value).FirstOrDefault());
                var Fac = await service.Agregar(recibido);



                var obj = new
                {
                    success = true,
                    mensaje = "",
                    documento = Fac
                };

                return new JsonResult(obj);

            }
            catch (ApiException ex)
            {

                Errores error = JsonConvert.DeserializeObject<Errores>(ex.Content.ToString());

                var obj = new
                {
                    success = false,
                    successAc = false,
                    mensaje = "Error en el exception: -> " + error.Message
                };
                return new JsonResult(obj);
            }
            catch (Exception ex)
            {


                var obj = new
                {
                    success = false,
                    successAc = false,
                    mensaje = "Error en el exception: -> " + ex.Message
                };
                return new JsonResult(obj);
            }
        }


        public async Task<IActionResult> OnPostPlanificadorMultiple(string recibidos)
        {
            try
            {
                AgendarMultipleModel recibido = new AgendarMultipleModel();
                var ms = new MemoryStream();
                await Request.Body.CopyToAsync(ms);

                byte[] compressedData = ms.ToArray();

                // Descomprimir los datos utilizando GZip
                using (var compressedStream = new MemoryStream(compressedData))
                using (var decompressedStream = new MemoryStream())
                {
                    using (var decompressionStream = new GZipStream(compressedStream, CompressionMode.Decompress))
                    {
                        decompressionStream.CopyTo(decompressedStream);
                    }

                    // Convertir los datos descomprimidos a una cadena JSON
                    var jsonString = System.Text.Encoding.UTF8.GetString(decompressedStream.ToArray());

                    // Procesar la cadena JSON como desees
                    // Por ejemplo, puedes deserializarla a un objeto C# utilizando Newtonsoft.Json
                    recibido = Newtonsoft.Json.JsonConvert.DeserializeObject<AgendarMultipleModel>(jsonString);
                }




                foreach (var cliente in recibido.clientes)
                {
                    ActividadesViewModel p = new ActividadesViewModel();
                    p.idEmpresa = Convert.ToInt32(cliente);
                    p.idUsuario = Convert.ToInt32(((ClaimsIdentity)User.Identity).Claims.Where(d => d.Type == ClaimTypes.NameIdentifier).Select(s1 => s1.Value).FirstOrDefault());
                    p.titulo = recibido.titulo;
                    p.fechaAgendada = recibido.fecha;
                    p.idTipoActividad = Convert.ToInt32(recibido.tipo);
                    p.comentario = recibido.titulo;
                    p.adjuntos_actividades = recibido.adjuntos_actividades;
                    var Fac = await service.Agregar(p);
                }

                var obj = new
                {
                    success = true,
                    mensaje = ""
                };

                return new JsonResult(obj);

            }
            catch (ApiException ex)
            {

                Errores error = JsonConvert.DeserializeObject<Errores>(ex.Content.ToString());

                var obj = new
                {
                    success = false,
                    successAc = false,
                    mensaje = "Error en el exception: -> " + error.Message
                };
                return new JsonResult(obj);
            }
            catch (Exception ex)
            {


                var obj = new
                {
                    success = false,
                    successAc = false,
                    mensaje = "Error en el exception: -> " + ex.Message
                };
                return new JsonResult(obj);
            }
        }


        public async Task<IActionResult> OnPostMarcarRealizado([FromBody] JsonElement data)
        {
            try
            {


                ActividadesViewModel p = new ActividadesViewModel();
                p.id = Convert.ToInt32(data.GetProperty("id").GetString());
                p.estado = "Realizado";
                await service.Editar(p);

                var obj = new
                {
                    success = true,
                    mensaje = ""
                };

                return new JsonResult(obj);

            }
            catch (ApiException ex)
            {

                Errores error = JsonConvert.DeserializeObject<Errores>(ex.Content.ToString());

                var obj = new
                {
                    success = false,
                    successAc = false,
                    mensaje = "Error en el exception: -> " + error.Message
                };
                return new JsonResult(obj);
            }
            catch (Exception ex)
            {


                var obj = new
                {
                    success = false,
                    successAc = false,
                    mensaje = "Error en el exception: -> " + ex.Message
                };
                return new JsonResult(obj);
            }
        }


    }
}
