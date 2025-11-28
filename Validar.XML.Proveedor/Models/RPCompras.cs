namespace Validar.XML.Proveedor.Models
{
    public class RPCompras
    {
        public string rncComprador { get; set; } = string.Empty;
        public string serie { get; set; } = string.Empty;
        public string serieModifica { get; set; } = string.Empty;
        public int numero { get; set; }
        public DateTime fechaEmision { get; set; }
        public string Cat06_idProveedorr { get; set; } = string.Empty;
        public string rncEmisor { get; set; } = string.Empty;
        public string razonSocialEmisor { get; set; } = string.Empty;
        public string cat02_id { get; set; } = string.Empty;
        public string montoTotal { get; set; } = string.Empty;
        public string totalItbis1 { get; set; } = string.Empty;
        public string totalItbis2 { get; set; } = string.Empty;
        public string totalItbis3 { get; set; } = string.Empty;
        public string montoGravadoTotal { get; set; } = string.Empty;
        public string montoGravadoI1 { get; set; } = string.Empty;
        public string montoGravadoI2 { get; set; } = string.Empty;
        public string montoGravadoI3 { get; set; } = string.Empty;
        public string montoExento { get; set; } = string.Empty;
        public string MontoImpuestoAdicional { get; set; } = string.Empty;
        public string MontoNoFacturable { get; set; } = string.Empty;
        public string MontoPeriodo { get; set; } = string.Empty;
        public string ValorPagar { get; set; } = string.Empty;
        public string ItbisRetenido { get; set; } = string.Empty;
        public string ItbisPercibido { get; set; } = string.Empty;
        public DateTime fechaRepcepcion { get; set; }
        public string created_by { get; set; } = string.Empty;
        public List<RPComprasDetalle> detalles { get; set; } = new List<RPComprasDetalle>();
    }
}
