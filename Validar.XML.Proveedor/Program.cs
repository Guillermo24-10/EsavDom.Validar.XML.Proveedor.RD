using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Serilog;
using Validar.XML.Proveedor.Services;
using Validar.XML.Proveedor.Services.Context;
using Validar.XML.Proveedor.Services.Repository;
using Validar.XML.Proveedor.Workers;

Console.Title = "Validador XML - Servicio de Procesamiento";
Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine(@"
╔══════════════════════════════════════════════════════════╗
║                                                          ║
║        VALIDADOR XML - SERVICIO DE PROCESAMIENTO         ║
║                                                          ║
╚══════════════════════════════════════════════════════════╝
");
Console.ResetColor();

// ========== CONFIGURAR SERILOG ==========
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information() // Cambiado a Information para evitar demasiados logs
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
    .WriteTo.Console(
        outputTemplate: "{Message:lj}{NewLine}", // Solo el mensaje, sin [INF]
        restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information
    )
    .WriteTo.File(
        "logs/log.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
    )
    .CreateLogger();
// ========================================


var builder = Host.CreateApplicationBuilder(args);

// == ESTA ES LA FORMA CORRECTA EN .NET 8 ==
builder.Logging.ClearProviders();
builder.Logging.AddFilter("*", LogLevel.None);
builder.Logging.AddSerilog();


// Configuración
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

// Registrar servicios
builder.Services.AddHttpClient();
builder.Services.AddSingleton<DapperContext>();
builder.Services.AddScoped<IBancoCajaService, BancoCajaService>();
builder.Services.AddScoped<IProveedoresService, ProveedoresService>();  
builder.Services.AddScoped<IComprasRepository, ComprasRepository>();
builder.Services.AddScoped<IBancoCajaRepository, BancoCajaRepository>();
builder.Services.AddScoped<IComprasService, ComprasService>();
builder.Services.AddScoped<IValidadorXmlService, ValidadorXmlService>();
builder.Services.AddSingleton<IStorageService, StorageService>();
builder.Services.AddSingleton<IColaService, ColaService>();
builder.Services.AddHostedService<ValidadorWorker>();

var host = builder.Build();

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine($"Fecha inicio: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
Console.ResetColor();

try
{
    await host.RunAsync();
}
finally
{
    Log.CloseAndFlush();
}
