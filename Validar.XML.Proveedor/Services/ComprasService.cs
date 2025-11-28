using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Xml;
using Validar.XML.Proveedor.Models;
using Validar.XML.Proveedor.Services.Repository;

namespace Validar.XML.Proveedor.Services
{
    public class ComprasService : IComprasService
    {
        private readonly IComprasRepository _comprasRepository;
        private readonly ILogger<ComprasService> _logger;

        public ComprasService(IComprasRepository comprasRepository, ILogger<ComprasService> logger)
        {
            _comprasRepository = comprasRepository;
            _logger = logger;
        }

        public async Task<bool> GuardarCompraAsync(XmlDocument xmlDoc)
        {
            try
            {
                var ns = new XmlNamespaceManager(xmlDoc.NameTable);
                ns.AddNamespace("ds", "http://www.w3.org/2000/09/xmldsig#");

                var eCF = xmlDoc.SelectSingleNode("//eNCF")?.InnerText ??
                           xmlDoc.SelectSingleNode("//NCF")?.InnerText ?? string.Empty;

                var nodosItems = xmlDoc.SelectNodes("//DetallesItems/Item");

                string fechaString = xmlDoc.SelectSingleNode("//FechaEmision")?.InnerText ?? string.Empty;
                DateTime.TryParse(fechaString, out DateTime tempDate);

                var detalles = new List<RPComprasDetalle>();

                if (nodosItems != null)
                {
                    foreach (XmlNode item in nodosItems)
                    {
                        var detalle = new RPComprasDetalle
                        {
                            cat01_id = xmlDoc.SelectSingleNode("//TipoeCF")?.InnerText ?? string.Empty,
                            item = int.TryParse(item.SelectSingleNode("NumeroLinea")?.InnerText, out int numLinea)
                           ? numLinea : 0,
                            cat07_id = item.SelectSingleNode(".//CodigoItem")?.InnerText ?? string.Empty,
                            nombreItem = item.SelectSingleNode("NombreItem")?.InnerText ?? string.Empty,
                            cat62_id = item.SelectSingleNode("IndicadorBienoServicio")?.InnerText ?? string.Empty,
                            cantidad = decimal.TryParse(item.SelectSingleNode("CantidadItem")?.InnerText, out decimal cant)
                               ? cant : 0,
                            cat03_id = item.SelectSingleNode("UnidadMedida")?.InnerText ?? string.Empty,
                            cat02_id = "DOP", // Este parece ser moneda, podrías usar el del nivel superior
                            precioUnitario = decimal.TryParse(item.SelectSingleNode("PrecioUnitarioItem")?.InnerText, out decimal precioUnit)
                                ? precioUnit : 0,
                            monto = decimal.TryParse(item.SelectSingleNode("MontoItem")?.InnerText, out decimal monto)
                            ? monto : 0,
                        };
                        detalles.Add(detalle);
                    }
                }

                var data = new RPCompras
                {
                    rncComprador = xmlDoc.SelectSingleNode("//RNCComprador")?.InnerText ??
                                  xmlDoc.SelectSingleNode("//Receptor/RNC")?.InnerText ??
                                  xmlDoc.SelectSingleNode("//Comprador/RNC")?.InnerText ?? string.Empty,
                    serie = eCF.Substring(0, 3),
                    serieModifica = "",
                    numero = int.Parse(eCF.Substring(3)),
                    fechaEmision = tempDate,
                    Cat06_idProveedorr = "2",
                    rncEmisor = xmlDoc.SelectSingleNode("//RNCEmisor")?.InnerText ??
                               xmlDoc.SelectSingleNode("//Emisor/RNC")?.InnerText ?? string.Empty,
                    razonSocialEmisor = xmlDoc.SelectSingleNode("//RazonSocialEmisor")?.InnerText ?? string.Empty,
                    cat02_id = "DOP",
                    montoTotal = xmlDoc.SelectSingleNode("//MontoTotal")?.InnerText ??
                                xmlDoc.SelectSingleNode("//TotalGeneral")?.InnerText ??
                                xmlDoc.SelectSingleNode("//Total")?.InnerText ?? string.Empty,
                    totalItbis1 = xmlDoc.SelectSingleNode("//TotalITBIS1")?.InnerText ?? string.Empty,
                    totalItbis2 = xmlDoc.SelectSingleNode("//TotalITBIS2")?.InnerText ?? string.Empty,
                    totalItbis3 = xmlDoc.SelectSingleNode("//TotalITBIS3")?.InnerText ?? string.Empty,
                    montoGravadoTotal = xmlDoc.SelectSingleNode("//MontoGravadoTotal")?.InnerText ?? string.Empty,
                    montoGravadoI1 = xmlDoc.SelectSingleNode("//MontoGravadoI1")?.InnerText ?? string.Empty,
                    montoGravadoI2 = xmlDoc.SelectSingleNode("//MontoGravadoI2")?.InnerText ?? string.Empty,
                    montoGravadoI3 = xmlDoc.SelectSingleNode("//MontoGravadoI3")?.InnerText ?? string.Empty,
                    montoExento = xmlDoc.SelectSingleNode("//MontoExento")?.InnerText ?? string.Empty,
                    MontoImpuestoAdicional = xmlDoc.SelectSingleNode("//MontoImpuestoAdicional")?.InnerText ?? string.Empty,
                    MontoNoFacturable = xmlDoc.SelectSingleNode("//MontoNoFacturable")?.InnerText ?? string.Empty,
                    MontoPeriodo = xmlDoc.SelectSingleNode("//MontoPeriodo")?.InnerText ?? string.Empty,
                    ValorPagar = xmlDoc.SelectSingleNode("//ValorPagar")?.InnerText ?? string.Empty,
                    ItbisRetenido = xmlDoc.SelectSingleNode("//TotalITBISRetenido")?.InnerText ?? string.Empty,
                    ItbisPercibido = xmlDoc.SelectSingleNode("//TotalITBISPercepcion")?.InnerText ?? string.Empty,
                    fechaRepcepcion = DateTime.Now,
                    created_by = "",
                    detalles = detalles

                };

                var result = await _comprasRepository.InsertarAsync(data);
                return result > 0;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error interno en BD");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al guardar la compra");
            }

            return false;
        }
    }
}
