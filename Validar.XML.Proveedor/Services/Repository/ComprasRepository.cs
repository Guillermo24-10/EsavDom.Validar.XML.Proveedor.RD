using Validar.XML.Proveedor.Models;
using Validar.XML.Proveedor.Services.Context;

namespace Validar.XML.Proveedor.Services.Repository
{
    public class ComprasRepository : IComprasRepository
    {
        public readonly DapperContext _context;

        public ComprasRepository(DapperContext context)
        {
            _context = context;
        }

        public Task<int> InsetarAsync(RPCompras compras)
        {
            throw new NotImplementedException();
        }
    }
}
