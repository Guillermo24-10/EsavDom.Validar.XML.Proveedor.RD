using Validar.XML.Proveedor.Models;

namespace Validar.XML.Proveedor.Services
{
    public interface IBancoCajaService
    {
        Task<IEnumerable<BancoCajaModel>> ListarBancoCaja(string emisor);
    }
}
