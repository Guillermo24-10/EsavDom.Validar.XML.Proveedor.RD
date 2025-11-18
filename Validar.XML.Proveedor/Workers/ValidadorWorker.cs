using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Validar.XML.Proveedor.Models;
using Validar.XML.Proveedor.Services;

namespace Validar.XML.Proveedor.Workers
{
    public class ValidadorWorker : BackgroundService
    {
        private readonly ILogger<ValidadorWorker> _logger;
        private readonly IConfiguration _configuration;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IValidadorXmlService _validadorService;
        private readonly IStorageService _storageService;
        private readonly IColaService _colaService;
        private readonly TimeSpan _intervaloConsulta;
        private readonly int _maximoMensajesPorCiclo;
        private int _totalProcesados = 0;
        private int _totalExitosos = 0;
        private int _totalErrores = 0;
        private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(2);

        private readonly string _containerName = "";
        private readonly string _baseStorage = "";

        public ValidadorWorker(
            ILogger<ValidadorWorker> logger,
            IConfiguration configuration,
            IServiceScopeFactory scopeFactory,
            IValidadorXmlService validadorService,
            IStorageService storageService,
            IColaService colaService)
        {
            _logger = logger;
            _configuration = configuration;
            _validadorService = validadorService;
            _storageService = storageService;
            _colaService = colaService;
            _scopeFactory = scopeFactory;

            var intervaloSegundos = configuration.GetValue("ValidadorConfig:IntervaloConsultaSegundos", 5);
            _intervaloConsulta = TimeSpan.FromSeconds(intervaloSegundos);
            _maximoMensajesPorCiclo = configuration.GetValue("ValidadorConfig:MaximoMensajesPorCiclo", 10);
            _containerName = configuration.GetValue<string>("ContenedorName")!;
            _baseStorage = configuration.GetValue<string>("BaseStorage")!;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            ConsolaMensaje("Esperando mensajes...");
            MostrarEstadisticas();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var mensajesProcesados = await ProcesarMensajesCola(stoppingToken);

                    if (mensajesProcesados == 0)
                    {
                        // No hay mensajes, esperar más tiempo
                        await Task.Delay(_intervaloConsulta, stoppingToken);
                    }
                    else
                    {
                        // Hay mensajes, esperar menos tiempo
                        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("detenido por cancelación");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en el ciclo principal del worker");
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }
            }

            ConsolaMensaje("Proceso finalizado. Mostrando estadísticas finales...");
            MostrarEstadisticasFinales();
        }

        private async Task<int> ProcesarMensajesCola(CancellationToken stoppingToken)
        {
            // Recibir múltiples mensajes a la vez
            var mensajes = await _colaService.RecibirMensajesAsync(_maximoMensajesPorCiclo, stoppingToken);

            if (mensajes == null || !mensajes.Any())
            {
                return 0; // No hay mensajes
            }
            ConsolaMensaje($"Iniciando procesamiento de {mensajes.Count} mensajes...");

            // Procesar en paralelo
            var tareasProcesamiento = mensajes.Select(mensaje =>
                ProcesarMensajeIndividualAsync(mensaje, stoppingToken));

            await Task.WhenAll(tareasProcesamiento);

            ConsolaMensaje($"Ciclo completado: {mensajes.Count} mensajes procesados");
            MostrarEstadisticas();

            return mensajes.Count;
        }

        private async Task ProcesarMensajeIndividualAsync(MensajeCola mensaje, CancellationToken stoppingToken)
        {
            var procesoExitoso = false;
            try
            {
                await ProcesarValidacion(mensaje.Datos, stoppingToken);
                procesoExitoso = true;

                Interlocked.Increment(ref _totalExitosos);
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _totalErrores);
                _logger.LogError(ex, "Error procesando mensaje: {MessageId}", mensaje.MessageId);

                if (mensaje.DequeueCount > 3)
                {
                    _logger.LogWarning("Mensaje movido a cola de errores después de {Count} intentos",
                        mensaje.DequeueCount);
                    await _colaService.MoverAColaErroresAsync(mensaje, ex.Message, stoppingToken);
                    procesoExitoso = true;
                }
            }
            finally
            {
                if (procesoExitoso)
                {
                    await _colaService.EliminarMensajeAsync(mensaje, stoppingToken);
                }

                Interlocked.Increment(ref _totalProcesados);
            }
        }

        private async Task ProcesarValidacion(MensajeValidacion datos, CancellationToken stoppingToken)
        {
            try
            {
                await _storageService.ActualizarEstadoAsync(
                    datos.EmisorId,
                    datos.IdProceso,
                    "Procesando",
                    string.Empty);

                var contenidoXml = await _storageService.DescargarBlobAsync(_containerName, $"{_baseStorage}/{datos.BlobXml}", stoppingToken);

                ConsolaMensaje("Validando XML...");
                var resultadoValidacion = await _validadorService.ValidarXmlAsync(contenidoXml, datos.EmisorId);

                if (resultadoValidacion.EsValido)
                {
                    await _storageService.ActualizarEstadoAsync(
                        datos.EmisorId,
                        datos.IdProceso,
                        "Aceptado",
                        string.Empty);

                    ConsolaMensaje($"XML válido: {datos.IdProceso}");
                }
                else
                {
                    var errores = string.Join("; ", resultadoValidacion.Errores);
                    await _storageService.ActualizarEstadoAsync(
                        datos.EmisorId,
                        datos.IdProceso,
                        "Rechazado",
                        errores);
                    await _storageService.EliminarBlobAsync(_containerName, $"{_baseStorage}/{datos.BlobPdf}", stoppingToken);
                    await _storageService.EliminarBlobAsync(_containerName, $"{_baseStorage}/{datos.BlobXml}", stoppingToken);
                    ConsolaMensaje($"XML rechazado: {datos.IdProceso}. Errores: {errores}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar validación: {IdProceso}", datos.IdProceso);

                await _storageService.ActualizarEstadoAsync(
                    datos.EmisorId,
                    datos.IdProceso,
                    "Error",
                    $"Error en procesamiento: {ex.Message}");

                throw;
            }
        }

        private void MostrarEstadisticas()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"Total procesados: {_totalProcesados} | Exitosos: {_totalExitosos} | Errores: {_totalErrores}");
            Console.ResetColor();
        }

        private void MostrarEstadisticasFinales()
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("=".PadRight(60, '='));
            Console.WriteLine("ESTADÍSTICAS FINALES");
            Console.WriteLine("=".PadRight(60, '='));
            Console.WriteLine($"Total procesados: {_totalProcesados}");
            Console.WriteLine($"Exitosos: {_totalExitosos}");
            Console.WriteLine($"Errores: {_totalErrores}");
            if (_totalProcesados > 0)
            {
                var tasaExito = (_totalExitosos * 100.0) / _totalProcesados;
                Console.WriteLine($"Tasa de éxito: {tasaExito:F2}%");
            }
            Console.WriteLine("=".PadRight(60, '='));
            Console.ResetColor();
        }
        private void ConsolaMensaje(string mensaje)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(mensaje);
            Console.ResetColor();
        }
    }
}
