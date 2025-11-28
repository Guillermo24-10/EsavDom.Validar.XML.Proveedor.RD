using Validar.XML.Proveedor.Models;
using Validar.XML.Proveedor.Services.Repository;

namespace Validar.XML.Proveedor.Services
{
    public class BancoCajaService : IBancoCajaService
    {
        private readonly IBancoCajaRepository _bancoCajaRepository;
        public BancoCajaService(IBancoCajaRepository bancoCajaRepository)
        {

            _bancoCajaRepository = bancoCajaRepository;
        }

        public async Task<IEnumerable<BancoCajaModel>> ListarBancoCaja(string emisor)
        {
            return await _bancoCajaRepository.ListarBancoCaja(emisor);
        }
    }
}
