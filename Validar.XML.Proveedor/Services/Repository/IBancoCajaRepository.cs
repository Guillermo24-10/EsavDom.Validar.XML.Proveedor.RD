using Validar.XML.Proveedor.Models;

namespace Validar.XML.Proveedor.Services.Repository
{
    public interface IBancoCajaRepository
    {
        Task<IEnumerable<BancoCajaModel>> ListarBancoCaja(string emisor);
    }
}
