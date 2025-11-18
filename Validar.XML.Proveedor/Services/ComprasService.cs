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

                string fechaString = xmlDoc.SelectSingleNode("//FechaEmision")?.InnerText ?? string.Empty;
                DateTime.TryParse(fechaString, out DateTime tempDate);

                var data = new RPCompras
                {
                    rncComprador = xmlDoc.SelectSingleNode("//RNCComprador")?.InnerText ??
                                  xmlDoc.SelectSingleNode("//Receptor/RNC")?.InnerText ??
                                  xmlDoc.SelectSingleNode("//Comprador/RNC")?.InnerText ?? string.Empty,
                    serie = eCF.Substring(0,3),
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
                    fechaRepcepcion = DateTime.Now,
                    created_by = ""

                };

                var result = await _comprasRepository.InsetarAsync(data);
                return result > 0;
            }
            catch(SqlException ex)
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
