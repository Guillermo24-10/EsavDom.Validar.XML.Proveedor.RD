using Microsoft.Extensions.Logging;
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

        public Task<bool> GuardarCompraAsync(RPCompras compra)
        {
            throw new NotImplementedException();
        }
    }
}
