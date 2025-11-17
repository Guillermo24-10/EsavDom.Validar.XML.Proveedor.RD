using Validar.XML.Proveedor.Models;

namespace Validar.XML.Proveedor.Services
{
    public interface IValidadorXmlService
    {
        Task<ResultadoValidacion> ValidarXmlAsync(string contenidoXml,string emisorId);
    }
}
