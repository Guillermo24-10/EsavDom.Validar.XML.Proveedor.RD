using Validar.XML.Proveedor.Models;

namespace Validar.XML.Proveedor.Services.Repository
{
    public interface IComprasRepository
    {
        Task<int> InsertarAsync(RPCompras compras);
    }
}
