using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Web;
using System.Xml;
using Validar.XML.Proveedor.Models;

namespace Validar.XML.Proveedor.Services
{
    public class ValidadorXmlService : IValidadorXmlService
    {
        private readonly ILogger<ValidadorXmlService> _logger;
        private readonly HttpClient _httpClient;
        private readonly string _ambiente;
        private readonly IComprasService _comprasService;

        public ValidadorXmlService(ILogger<ValidadorXmlService> logger, HttpClient httpClient, IConfiguration configuration, IComprasService comprasService)
        {
            _logger = logger;
            _httpClient = httpClient;
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            _ambiente = configuration.GetValue<string>("Ambiente")!;
            _comprasService = comprasService;
        }

        public async Task<ResultadoValidacion> ValidarXmlAsync(string contenidoXml, string emisorId)
        {
            var resultado = new ResultadoValidacion { EsValido = true };

            try
            {
                contenidoXml = contenidoXml.TrimStart('\uFEFF', '\u200B', '\u0000');
                // 1. Validar estructura básica del XML
                if (string.IsNullOrWhiteSpace(contenidoXml))
                {
                    resultado.EsValido = false;
                    resultado.Errores.Add("El contenido XML está vacío");
                    return resultado;
                }

                var xmlDoc = new XmlDocument();
                try
                {
                    xmlDoc.LoadXml(contenidoXml);
                }
                catch (XmlException ex)
                {
                    resultado.EsValido = false;
                    resultado.Errores.Add($"XML mal formado: {ex.Message}");
                    return resultado;
                }

                if (xmlDoc.DocumentElement == null)
                {
                    resultado.EsValido = false;
                    resultado.Errores.Add("El XML no tiene elemento raíz");
                    return resultado;
                }

                // 2. Validar estructura y campos requeridos
                ValidarEstructuraXml(xmlDoc, resultado, emisorId);
                if (!resultado.EsValido) return resultado;

                ValidarCamposRequeridos(xmlDoc, resultado);
                if (!resultado.EsValido) return resultado;

                ValidarFormatos(xmlDoc, resultado);
                if (!resultado.EsValido) return resultado;

                // 3. Extraer datos para consulta DGII
                var datosConsulta = ExtraerDatosParaDGII(xmlDoc);
                if (datosConsulta == null)
                {
                    resultado.EsValido = false;
                    resultado.Errores.Add("No se pudieron extraer los datos necesarios para consulta DGII");
                    return resultado;
                }

                // 4. Consultar en DGII
                _logger.LogInformation("Consultando DGII - RNC: {RncEmisor}, ENCF: {ENCF}",
                    datosConsulta.RncEmisor, datosConsulta.ENCF);

                var resultadoDGII = await ConsultarTimbreDGII(datosConsulta);

                if (!resultadoDGII.Exitoso)
                {
                    resultado.EsValido = false;
                    resultado.Errores.Add($"Validación DGII falló: {resultadoDGII.Mensaje}");
                    resultado.Detalles["EstadoDGII"] = resultadoDGII.Estado;
                    resultado.Detalles["MensajeDGII"] = resultadoDGII.Mensaje;
                    return resultado;
                }

                // 5. Agregar información adicional
                resultado.Detalles["NodoRaiz"] = xmlDoc.DocumentElement.Name;
                resultado.Detalles["NumeroNodos"] = xmlDoc.SelectNodes("//*")?.Count.ToString() ?? "0";
                resultado.Detalles["EstadoDGII"] = resultadoDGII.Estado;
                resultado.Detalles["ValidadoPorDGII"] = "Sí";
                resultado.Detalles["FechaValidacionDGII"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

                // Guardar RPCompras si es que no tuvo rechazos previos
                var status = await _comprasService.GuardarCompraAsync(xmlDoc); // guarda bd registro de compra
                if (status)
                {
                    _logger.LogInformation("Registro de compra guardado exitosamente en RPCompras.");
                }
                //_logger.LogInformation("Validación completa exitosa - DGII: {Estado}", resultadoDGII.Estado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en validación XML");
                resultado.EsValido = false;
                resultado.Errores.Add($"Error interno: {ex.Message}");
            }

            return resultado;
        }

        private DatosConsultaDGII? ExtraerDatosParaDGII(XmlDocument xmlDoc)
        {
            try
            {
                var ns = new XmlNamespaceManager(xmlDoc.NameTable);
                ns.AddNamespace("ds", "http://www.w3.org/2000/09/xmldsig#");

                var signatureValueNode = xmlDoc.SelectSingleNode("//ds:SignatureValue", ns);
                string signatureValue = signatureValueNode?.InnerText ?? string.Empty;

                string codigoSeguridad = string.Empty;
                if (!string.IsNullOrEmpty(signatureValue) && signatureValue.Length >= 6)
                    codigoSeguridad = signatureValue.Substring(0, 6);

                var datos = new DatosConsultaDGII
                {
                    RncEmisor = xmlDoc.SelectSingleNode("//RNCEmisor")?.InnerText ??
                               xmlDoc.SelectSingleNode("//Emisor/RNC")?.InnerText ?? string.Empty,

                    RncComprador = xmlDoc.SelectSingleNode("//RNCComprador")?.InnerText ??
                                  xmlDoc.SelectSingleNode("//Receptor/RNC")?.InnerText ??
                                  xmlDoc.SelectSingleNode("//Comprador/RNC")?.InnerText ?? string.Empty,

                    ENCF = xmlDoc.SelectSingleNode("//eNCF")?.InnerText ??
                           xmlDoc.SelectSingleNode("//NCF")?.InnerText ?? string.Empty,

                    FechaEmision = xmlDoc.SelectSingleNode("//FechaEmision")?.InnerText ??
                                  xmlDoc.SelectSingleNode("//Fecha")?.InnerText ?? string.Empty,

                    MontoTotal = xmlDoc.SelectSingleNode("//MontoTotal")?.InnerText ??
                                xmlDoc.SelectSingleNode("//TotalGeneral")?.InnerText ??
                                xmlDoc.SelectSingleNode("//Total")?.InnerText ?? string.Empty,

                    FechaFirma = xmlDoc.SelectSingleNode("//FechaFirma")?.InnerText ??
                                xmlDoc.SelectSingleNode("//FechaHoraFirma")?.InnerText ?? string.Empty,

                    CodigoSeguridad = codigoSeguridad
                };

                if (string.IsNullOrEmpty(datos.RncEmisor) ||
                    string.IsNullOrEmpty(datos.ENCF) ||
                    string.IsNullOrEmpty(datos.FechaEmision))
                {
                    _logger.LogWarning(" Faltan datos obligatorios para consulta DGII");
                    return null;
                }

                //Console.ForegroundColor = ConsoleColor.Green;
                //Console.WriteLine($"Datos extraídos - RNC: {datos.RncEmisor}, ENCF: {datos.ENCF}, Monto: {datos.MontoTotal}");

                return datos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al extraer datos para DGII");
                return null;
            }
        }

        private async Task<ResultadoConsultaDGII> ConsultarTimbreDGII(DatosConsultaDGII datos)
        {
            try
            {
                // Construir URL con parámetros
                var baseUrl = $"https://ecf.dgii.gov.do/{_ambiente}/ConsultaTimbre";

                var queryParams = HttpUtility.ParseQueryString(string.Empty);
                queryParams["RncEmisor"] = datos.RncEmisor;
                queryParams["RncComprador"] = datos.RncComprador;
                queryParams["ENCF"] = datos.ENCF;
                queryParams["FechaEmision"] = datos.FechaEmision;
                queryParams["MontoTotal"] = datos.MontoTotal;
                queryParams["FechaFirma"] = datos.FechaFirma;
                queryParams["CodigoSeguridad"] = datos.CodigoSeguridad;

                var url = $"{baseUrl}?{queryParams}";

                //Console.WriteLine($"Consultando DGII en: {url}");

                // Realizar consulta HTTP
                var response = await _httpClient.GetAsync(url);
                var contenido = await response.Content.ReadAsStringAsync();

                //Console.WriteLine($"Respuesta DGII Status: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    // Parsear respuesta - ajustar según formato real de DGII
                    return ParsearRespuestaDGII(contenido, response.StatusCode);
                }
                else
                {
                    return new ResultadoConsultaDGII
                    {
                        Exitoso = false,
                        Estado = "Error HTTP",
                        Mensaje = $"Error HTTP {response.StatusCode}: {contenido}"
                    };
                }
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Timeout al consultar DGII");
                return new ResultadoConsultaDGII
                {
                    Exitoso = false,
                    Estado = "Timeout",
                    Mensaje = "Timeout al consultar servicio DGII"
                };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error HTTP al consultar DGII");
                return new ResultadoConsultaDGII
                {
                    Exitoso = false,
                    Estado = "Error Conexión",
                    Mensaje = $"Error de conexión: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al consultar DGII");
                return new ResultadoConsultaDGII
                {
                    Exitoso = false,
                    Estado = "Error",
                    Mensaje = $"Error: {ex.Message}"
                };
            }
        }

        private ResultadoConsultaDGII ParsearRespuestaDGII(string contenido, System.Net.HttpStatusCode statusCode)
        {
            try
            {
                if (contenido.Contains("Aceptado", StringComparison.OrdinalIgnoreCase))
                {
                    return new ResultadoConsultaDGII
                    {
                        Exitoso = true,
                        Estado = "Aceptado",
                        Mensaje = "Comprobante válido según DGII",
                        ContenidoRespuesta = contenido
                    };
                }
                else if (!contenido.Contains("Aceptado", StringComparison.OrdinalIgnoreCase))
                {
                    return new ResultadoConsultaDGII
                    {
                        Exitoso = false,
                        Estado = "Rechazado",
                        Mensaje = "Comprobante NO válido o rechazado por DGII",
                        ContenidoRespuesta = contenido
                    };
                }
                else if (contenido.Contains("NO ENCONTRADO", StringComparison.OrdinalIgnoreCase))
                {
                    return new ResultadoConsultaDGII
                    {
                        Exitoso = false,
                        Estado = "NO ENCONTRADO",
                        Mensaje = "Comprobante no encontrado en DGII",
                        ContenidoRespuesta = contenido
                    };
                }
                else
                {

                    return new ResultadoConsultaDGII
                    {
                        Exitoso = false,
                        Estado = "INDETERMINADO",
                        Mensaje = "No se pudo determinar el estado del comprobante",
                        ContenidoRespuesta = contenido
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al parsear respuesta DGII");
                return new ResultadoConsultaDGII
                {
                    Exitoso = false,
                    Estado = "Error Parseo",
                    Mensaje = $"Error al interpretar respuesta: {ex.Message}",
                    ContenidoRespuesta = contenido
                };
            }
        }

        private void ValidarEstructuraXml(XmlDocument xmlDoc, ResultadoValidacion resultado, string emisorId)
        {
            try
            {
                // 1. Obtener el RNC del XML (probando varias rutas)
                string? rncCompradorXml =
                    xmlDoc.SelectSingleNode("//RNCComprador")?.InnerText ??
                                  xmlDoc.SelectSingleNode("//Receptor/RNC")?.InnerText ??
                                  xmlDoc.SelectSingleNode("//Comprador/RNC")?.InnerText ?? string.Empty;

                if (string.IsNullOrEmpty(rncCompradorXml))
                {
                    resultado.Errores.Add("No se encontró el RNC del comprador en el XML.");
                    resultado.EsValido = false;
                    return;
                }

                // 2. Agregarlo a la lista de detalles para trazabilidad
                resultado.Detalles["RNC_Xml"] = rncCompradorXml;
                resultado.Detalles["RNC_Cola"] = emisorId;

                // 3. Validar que coincida con el enviado en la cola
                if (!string.Equals(rncCompradorXml, emisorId, StringComparison.OrdinalIgnoreCase))
                {
                    resultado.Errores.Add($"El RNC del XML ({rncCompradorXml}) no coincide con el RNC emisor en la cola ({emisorId}).");
                    resultado.EsValido = false;
                    return;
                }

                // 4. Si todo ok, marcar válido
                resultado.EsValido = true;
            }
            catch (Exception ex)
            {
                resultado.Errores.Add("Error validando estructura XML: " + ex.Message);
                resultado.EsValido = false;
            }
        }

        private void ValidarCamposRequeridos(XmlDocument xmlDoc, ResultadoValidacion resultado)
        {
            var ns = new XmlNamespaceManager(xmlDoc.NameTable);
            ns.AddNamespace("ds", "http://www.w3.org/2000/09/xmldsig#");

            var xpathRequeridos = new Dictionary<string, string>()
            {
                { "RNCEmisor", "//RNCEmisor" },
                { "eNCF", "//eNCF" },
                { "FechaEmision", "//FechaEmision" },
                { "SignatureValue", "//ds:SignatureValue" }
            };

            foreach (var item in xpathRequeridos)
            {
                var nodo = xmlDoc.SelectSingleNode(item.Value, ns);

                if (nodo == null || string.IsNullOrWhiteSpace(nodo.InnerText))
                {
                    resultado.EsValido = false;
                    resultado.Errores.Add($"Falta elemento requerido: {item.Key}");
                }
            }
        }

        private void ValidarFormatos(XmlDocument xmlDoc, ResultadoValidacion resultado)
        {
            // Validar formatos específicos
        }
    }
}
