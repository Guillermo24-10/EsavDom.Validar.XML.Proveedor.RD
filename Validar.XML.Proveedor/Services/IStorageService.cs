using Azure.Data.Tables;

namespace Validar.XML.Proveedor.Services
{
    public interface IStorageService
    {
        Task<string> DescargarBlobAsync(string containerName, string blobName, CancellationToken cancellationToken);
        Task ActualizarEstadoAsync(string emisorId, string idProceso, string estado, string mensajeError);
        Task EliminarBlobAsync(string containerName, string blobName, CancellationToken cancellationToken);
    }
}
