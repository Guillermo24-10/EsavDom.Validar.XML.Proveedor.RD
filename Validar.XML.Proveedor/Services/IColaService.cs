using Validar.XML.Proveedor.Models;

namespace Validar.XML.Proveedor.Services
{
    public interface IColaService
    {
        Task<List<MensajeCola>> RecibirMensajesAsync(int maxMessages, CancellationToken cancellationToken);
        Task<MensajeCola?> RecibirMensajeAsync(CancellationToken cancellationToken);

        Task EliminarMensajeAsync(MensajeCola mensaje, CancellationToken cancellationToken);
        Task MoverAColaErroresAsync(MensajeCola mensaje, string errorMessage, CancellationToken cancellationToken);
    }
}
