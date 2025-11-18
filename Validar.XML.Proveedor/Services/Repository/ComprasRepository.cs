using Dapper;
using Microsoft.Extensions.Logging;
using Validar.XML.Proveedor.Models;
using Validar.XML.Proveedor.Services.Context;

namespace Validar.XML.Proveedor.Services.Repository
{
    public class ComprasRepository : IComprasRepository
    {
        public readonly DapperContext _context;
        public readonly ILogger<ComprasRepository> _logger;

        public ComprasRepository(DapperContext context, ILogger<ComprasRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<int> InsetarAsync(RPCompras compras)
        {
            var result = 0;
            try
            {
                using var connection = _context.CreateConnection();

                var query = "spTransaccion_ReporteEsavCompras_Guardar";
                var parameters = new DynamicParameters();
                parameters.Add("@Comprador_id", compras.rncComprador);
                parameters.Add("@serie_id", compras.serie);
                parameters.Add("@serie_idModifica", compras.serieModifica);
                parameters.Add("@numero", compras.numero);
                parameters.Add("@FechaEmision", compras.fechaEmision);
                parameters.Add("@Cat06_idProveedor", "2");
                parameters.Add("@Cat02_ID", compras.cat02_id);
                parameters.Add("@IDProveedor", compras.rncEmisor);
                parameters.Add("@RazonSocialProveedor", compras.razonSocialEmisor);
                parameters.Add("@MontoTotal", compras.montoTotal);
                parameters.Add("@AcuseRecibo", compras.fechaRepcepcion);
                parameters.Add("@AprobacionComercial", false);
                parameters.Add("@created_by", compras.created_by);

                result = await connection.ExecuteAsync(query, parameters, commandType: System.Data.CommandType.StoredProcedure);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inserting RPCompras record");
            }

            return result;
        }
    }
}
