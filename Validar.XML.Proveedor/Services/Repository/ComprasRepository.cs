using Dapper;
using Microsoft.Extensions.Logging;
using System.Data;
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

        public async Task<int> InsertarAsync(RPCompras compras)
        {
            if (compras == null)
                throw new ArgumentNullException(nameof(compras));

            try
            {
                using var connection = _context.CreateConnection();
                connection.Open();

                using var transaction = connection.BeginTransaction();

                try
                {
                    // Insertar cabecera
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
                    parameters.Add("@MontoTotal", string.IsNullOrWhiteSpace(compras.montoTotal) ? 0 : Convert.ToDecimal(compras.montoTotal));
                    parameters.Add("@TotalItbis1", string.IsNullOrWhiteSpace(compras.totalItbis1) ? 0 : Convert.ToDecimal(compras.totalItbis1));
                    parameters.Add("@TotalItbis2", string.IsNullOrWhiteSpace(compras.totalItbis2) ? 0 : Convert.ToDecimal(compras.totalItbis2));
                    parameters.Add("@TotalItbis3", string.IsNullOrWhiteSpace(compras.totalItbis3) ? 0 : Convert.ToDecimal(compras.totalItbis3));
                    parameters.Add("@MontoGravadoTotal", string.IsNullOrWhiteSpace(compras.montoGravadoTotal) ? 0 : Convert.ToDecimal(compras.montoGravadoTotal));
                    parameters.Add("@MontoGravadoI1", string.IsNullOrWhiteSpace(compras.montoGravadoI1) ? 0 : Convert.ToDecimal(compras.montoGravadoI1));
                    parameters.Add("@MontoGravadoI2", string.IsNullOrWhiteSpace(compras.montoGravadoI2) ? 0 : Convert.ToDecimal(compras.montoGravadoI2));
                    parameters.Add("@MontoGravadoI3", string.IsNullOrWhiteSpace(compras.montoGravadoI3) ? 0 : Convert.ToDecimal(compras.montoGravadoI3));
                    parameters.Add("@MontoExento", string.IsNullOrWhiteSpace(compras.montoExento) ? 0 : Convert.ToDecimal(compras.montoExento));
                    parameters.Add("@MontoImpuestoAdicional", string.IsNullOrWhiteSpace(compras.MontoImpuestoAdicional) ? 0 : Convert.ToDecimal(compras.MontoImpuestoAdicional));
                    parameters.Add("@MontoNoFacturable", string.IsNullOrWhiteSpace(compras.MontoNoFacturable) ? 0 : Convert.ToDecimal(compras.MontoNoFacturable));
                    parameters.Add("@MontoPeriodo", string.IsNullOrWhiteSpace(compras.MontoPeriodo) ? 0 : Convert.ToDecimal(compras.MontoPeriodo));
                    parameters.Add("@ValorPagar", string.IsNullOrWhiteSpace(compras.ValorPagar) ? 0 : Convert.ToDecimal(compras.ValorPagar));
                    parameters.Add("@ItbisRetenido", string.IsNullOrWhiteSpace(compras.ItbisRetenido) ? 0 : Convert.ToDecimal(compras.ItbisRetenido));
                    parameters.Add("@ItbisPercibido", string.IsNullOrWhiteSpace(compras.ItbisPercibido) ? 0 : Convert.ToDecimal(compras.ItbisPercibido));
                    parameters.Add("@AcuseRecibo", compras.fechaRepcepcion);
                    parameters.Add("@AprobacionComercial", false);
                    parameters.Add("@created_by", compras.created_by);

                    //foreach (var p in parameters.ParameterNames) 
                    //{
                    //    Console.WriteLine($"{p} = {parameters.Get<dynamic>(p)}"); // para ver los parametros que se envia
                    //}

                    await connection.ExecuteAsync(
                        query,
                        parameters,
                        commandType: CommandType.StoredProcedure,
                        transaction: transaction
                    );

                    // Insertar detalles
                    if (compras.detalles?.Count > 0)
                    {
                        var sp = "spTransaccion_Reporte_EsavcomprasDetalle_Guardar";

                        foreach (var item in compras.detalles)
                        {
                            item.numero = compras.numero;
                            item.serie_id = compras.serie;
                            item.emisor_id = compras.rncEmisor;

                            var param = new DynamicParameters();
                            param.Add("@emisor_id", item.emisor_id);
                            param.Add("@serie_id", item.serie_id);
                            param.Add("@numero", item.numero);
                            param.Add("@cat01_id", item.cat01_id);
                            param.Add("@item", item.item);
                            param.Add("@cat07_id", item.cat07_id);
                            param.Add("@cat23_id", item.cat23_id);
                            param.Add("@montoITBISRetenido", item.montoITBISRetenido);
                            param.Add("@montoRetencionRenta", item.montoRetencionRenta);
                            param.Add("@nombreItem", item.nombreItem);
                            param.Add("@cat62_id", item.cat62_id);
                            param.Add("@descripcionAdicional", item.descripcionAdicional);
                            param.Add("@cantidad", item.cantidad);
                            param.Add("@cat03_id", item.cat03_id);
                            param.Add("@cantidadReferencial", item.cantidadReferencial);
                            param.Add("@cat03_idReferencial", item.cat03_idReferencial);
                            param.Add("@gradosAlcohol", item.gradosAlcohol);
                            param.Add("@cantidadMililitros", item.cantidadMililitros);
                            param.Add("@precioUnitarioReferencia", item.precioUnitarioReferencia);

                            if (item.fechaElaboracion != DateTime.MinValue)
                                param.Add("@fechaElaboracion", item.fechaElaboracion);
                            if (item.fechaVencimiento != DateTime.MinValue)
                                param.Add("@fechaVencimiento", item.fechaVencimiento);

                            param.Add("@precioUnitario", item.precioUnitario);
                            param.Add("@montoDescuento", item.montoDescuento);
                            param.Add("@montoRecargo", item.montoRecargo);
                            param.Add("@cat02_id", item.cat02_id);
                            param.Add("@monto", item.monto);

                            await connection.ExecuteAsync(
                                sp,
                                param,
                                commandType: CommandType.StoredProcedure,
                                transaction: transaction
                            );
                        }
                    }

                    transaction.Commit();
                    return 1;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    _logger.LogError(ex, "Error en transacción INSERT para compra {Numero}", compras.numero);
                    return 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al insertar registro de compras");
                return 0;
            }
        }
    }
}
