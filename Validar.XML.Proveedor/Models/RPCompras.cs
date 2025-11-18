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
        public DateTime fechaRepcepcion { get; set; }
        public string created_by { get; set; } = string.Empty;
    }
}
