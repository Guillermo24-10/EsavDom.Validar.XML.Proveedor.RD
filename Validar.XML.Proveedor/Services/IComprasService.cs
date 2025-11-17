using Validar.XML.Proveedor.Models;

namespace Validar.XML.Proveedor.Services
{
    internal interface IComprasService
    {
        Task<bool> GuardarCompraAsync(RPCompras compra);
    }
}
