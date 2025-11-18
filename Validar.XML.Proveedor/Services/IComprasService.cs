using System.Xml;
using Validar.XML.Proveedor.Models;

namespace Validar.XML.Proveedor.Services
{
    public interface IComprasService
    {
        Task<bool> GuardarCompraAsync(XmlDocument xmlDoc);
    }
}
