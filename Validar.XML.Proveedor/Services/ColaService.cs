using Azure.Storage.Queues;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Validar.XML.Proveedor.Models;

namespace Validar.XML.Proveedor.Services
{
    public class ColaService : IColaService
    {
        private readonly QueueClient _queueClient;
        private readonly QueueClient _poisonQueueClient;
        private readonly ILogger<ColaService> _logger;

        public ColaService(IConfiguration configuration, ILogger<ColaService> logger)
        {
            var connectionString = configuration.GetConnectionString("ConnStorageDom");
            _queueClient = new QueueClient(connectionString, "validacion-xml");
            _poisonQueueClient = new QueueClient(connectionString, "validaciones-xml-errores");
            _logger = logger;

            // Crear colas si no existen
            _queueClient.CreateIfNotExists();
            _poisonQueueClient.CreateIfNotExists();
        }

        public async Task<List<MensajeCola>> RecibirMensajesAsync(int maxMessages, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _queueClient.ReceiveMessagesAsync(
                    maxMessages: maxMessages,
                    visibilityTimeout: TimeSpan.FromMinutes(2),
                    cancellationToken: cancellationToken);

                if (response.Value == null || response.Value.Length == 0)
                {
                    return new List<MensajeCola>();
                }

                var mensajes = new List<MensajeCola>();

                foreach (var mensaje in response.Value)
                {
                    try
                    {
                        // Decodificar mensaje si es necesario
                        string messageText = mensaje.MessageText;
                        try
                        {
                            var bytes = Convert.FromBase64String(mensaje.MessageText);
                            messageText = System.Text.Encoding.UTF8.GetString(bytes);
                        }
                        catch { }

                        var datos = JsonSerializer.Deserialize<MensajeValidacion>(messageText);

                        if (datos == null)
                        {
                            _logger.LogWarning("Mensaje con formato inválido: {MessageId}", mensaje.MessageId);
                            await EliminarMensajeAsync(new MensajeCola { MessageId = mensaje.MessageId, PopReceipt = mensaje.PopReceipt }, cancellationToken);
                            continue;
                        }

                        mensajes.Add(new MensajeCola
                        {
                            MessageId = mensaje.MessageId,
                            PopReceipt = mensaje.PopReceipt,
                            MessageText = messageText,
                            DequeueCount = (int)mensaje.DequeueCount,
                            Datos = datos
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error procesando mensaje individual {MessageId}", mensaje.MessageId);
                    }
                }

                return mensajes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al recibir mensajes de la cola");
                return new List<MensajeCola>();
            }
        }

        public async Task EliminarMensajeAsync(MensajeCola mensaje, CancellationToken cancellationToken)
        {
            try
            {
                await _queueClient.DeleteMessageAsync(mensaje.MessageId, mensaje.PopReceipt, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar mensaje {MessageId}", mensaje.MessageId);
                throw;
            }
        }
        public async Task<MensajeCola?> RecibirMensajeAsync(CancellationToken cancellationToken)
        {
            var mensajes = await RecibirMensajesAsync(1, cancellationToken);
            return mensajes.FirstOrDefault();
        }

        public async Task MoverAColaErroresAsync(MensajeCola mensaje, string errorMessage, CancellationToken cancellationToken)
        {
            try
            {
                await _poisonQueueClient.SendMessageAsync(mensaje.MessageText, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al mover mensaje {MessageId} a cola de veneno", mensaje.MessageId);
            }
        }
    }
}
