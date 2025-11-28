using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Validar.XML.Proveedor.Services
{
    public class ProveedoresService : IProveedoresService
    {
 
        private readonly ILogger<ProveedoresService> _logger;
        private readonly string _connectionString;
        public ProveedoresService(IConfiguration configuration, ILogger<ProveedoresService> logger)
        {
            _logger = logger;
            _connectionString = configuration.GetConnectionString("ConnStorageDom")
                ?? throw new InvalidOperationException("ConnectionString 'ConnStorageDom' no encontrado");
        }

        public async Task<bool> GuardarProveedores(List<Models.Proveedor> proveedores, int accion)
        {
            try
            {
                if (proveedores == null || proveedores.Count == 0)
                    return false;

                var table = new TableClient(_connectionString, $"PROVEEDORES{proveedores.First().RncComprador}");
                await table.CreateIfNotExistsAsync();

                const int batchSize = 100;

                for (int i = 0; i < proveedores.Count; i += batchSize)
                {
                    var subset = proveedores.Skip(i).Take(batchSize);

                    var acciones = new List<TableTransactionAction>();

                    foreach (var item in subset)
                    {
                        var entidad = new TableEntity("PROVEEDORES", item.RncProveedor)
                        {
                            ["RncComprador"] = item.RncComprador ?? "",
                            ["RazonSocialProveedor"] = item.RazonSocialProveedor ?? "",
                            ["TipoDocProveedor"] = item.TipoDocProveedor ?? "",
                            ["Telefono"] = item.Telefono ?? "",
                            ["RncProveedor"] = item.RncProveedor ?? "",
                            ["CorreoPrincial"] = item.CorreoPrincial ?? "",
                            ["CorreCopia"] = item.CorreoCopia ?? "",
                            ["NumeroCuenta"] = item.Cuenta ?? ""
                        };

                        if (accion == 1 || accion == 2)
                        {
                            acciones.Add(new TableTransactionAction(TableTransactionActionType.UpsertReplace, entidad));
                        }
                        else if (accion == 3)
                        {
                            acciones.Add(new TableTransactionAction(TableTransactionActionType.Delete, entidad));
                        }
                    }

                    if (acciones.Count > 0)
                        await table.SubmitTransactionAsync(acciones);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,"GuardarProveedores");
                return false;
            }
        }

    }
}
