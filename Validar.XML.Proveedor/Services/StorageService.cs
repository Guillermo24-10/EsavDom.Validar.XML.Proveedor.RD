using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Validar.XML.Proveedor.Services
{
    public class StorageService : IStorageService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<StorageService> _logger;
        private readonly string _connectionString;

        public StorageService(IConfiguration configuration, ILogger<StorageService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _connectionString = _configuration.GetConnectionString("ConnStorageDom")
                ?? throw new InvalidOperationException("ConnectionString 'ConnStorageDom' no encontrado");
        }

        public async Task<string> DescargarBlobAsync(string containerName, string blobName, CancellationToken cancellationToken)
        {
            try
            {
                var blobService = new BlobServiceClient(_connectionString);
                var container = blobService.GetBlobContainerClient(containerName);
                var blobClient = container.GetBlobClient(blobName);

                var response = await blobClient.DownloadContentAsync(cancellationToken);
                return response.Value.Content.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al descargar blob {BlobName}", blobName);
                throw;
            }
        }
        public async Task ActualizarEstadoAsync(string emisorId, string idProceso, string estado, string mensajeError)
        {
            try
            {
                var tableClient = new TableClient(_connectionString, "DocumentosValidacion");
                await tableClient.CreateIfNotExistsAsync();

                var entidad = await tableClient.GetEntityAsync<TableEntity>(emisorId, idProceso);

                entidad.Value["Estado"] = estado;
                entidad.Value["MensajeError"] = mensajeError ?? string.Empty;
                entidad.Value["FechaActualizacion"] = DateTime.UtcNow;

                if (estado == "Error" || estado == "Rechazado")
                {
                    var reintentos = entidad.Value.ContainsKey("Reintentos")
                        ? (int)entidad.Value["Reintentos"]
                        : 0;
                    entidad.Value["Reintentos"] = reintentos + 1;
                }

                await tableClient.UpdateEntityAsync(entidad.Value, entidad.Value.ETag, TableUpdateMode.Replace);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar estado: {IdProceso}", idProceso);
                throw;
            }
        }

        public Task EliminarBlobAsync(string containerName, string blobName, CancellationToken cancellationToken)
        {
            try
            {
                var blobService = new BlobServiceClient(_connectionString);
                var container = blobService.GetBlobContainerClient(containerName);
                var blobClient = container.GetBlobClient(blobName);

                return blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar blob {BlobName}", blobName);
                throw;
            }
        }
    }
}
