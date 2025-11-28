using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace Validar.XML.Proveedor.Services.Context
{
    public class DapperContext
    {
        private readonly string _connectionString;
        private readonly string _connectionStringEsavDomConfig;

        public DapperContext(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("ConnDatabase")
                ?? throw new InvalidOperationException("ConnectionString 'ConnDatabase' no encontrado");
            _connectionStringEsavDomConfig = configuration.GetConnectionString("ConnDatabaseConfig")
                ?? throw new InvalidOperationException("ConnectionString 'ConnEsavDomConfig' no encontrado");
        }

        public IDbConnection CreateConnection() => new SqlConnection(_connectionString);
        public IDbConnection CreateConnectionEsavDomConfig() => new SqlConnection(_connectionStringEsavDomConfig);
    }


}
