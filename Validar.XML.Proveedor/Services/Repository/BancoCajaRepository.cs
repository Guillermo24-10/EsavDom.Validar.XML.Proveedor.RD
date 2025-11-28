using Dapper;
using Microsoft.Extensions.Logging;
using Validar.XML.Proveedor.Models;
using Validar.XML.Proveedor.Services.Context;

namespace Validar.XML.Proveedor.Services.Repository
{
    public class BancoCajaRepository : IBancoCajaRepository
    {
        private readonly DapperContext _context;
        private readonly ILogger<BancoCajaRepository> _logger;

        public BancoCajaRepository(DapperContext context, ILogger<BancoCajaRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<BancoCajaModel>> ListarBancoCaja(string emisor)
        {
            try
            {
                using (var con = _context.CreateConnectionEsavDomConfig())
                {
                    var query = "sp_BancoCaja_CRUD";
                    var param = new DynamicParameters();
                    param.Add("@Accion", 4);
                    param.Add("@Banc_IdEmisor", emisor);

                    var result = await con.QueryAsync<BancoCajaModel>(query, param, commandType: System.Data.CommandType.StoredProcedure);
                    return result.ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar BancoCaja para el emisor {Emisor}", emisor);
                return Enumerable.Empty<BancoCajaModel>();
            }
        }
    }
}
