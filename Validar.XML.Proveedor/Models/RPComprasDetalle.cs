namespace Validar.XML.Proveedor.Models
{
    public class RPComprasDetalle
    {
        public string emisor_id { get; set; } = string.Empty;
        public string serie_id { get; set; } = string.Empty;    
        public int numero { get; set; }
        public string cat01_id { get; set; } = string.Empty;
        public int item { get; set; }
        public string cat07_id { get; set; } = string.Empty;
        public string cat23_id { get; set; } = string.Empty;
        public decimal montoITBISRetenido { get; set; }
        public decimal montoRetencionRenta { get; set; }
        public string nombreItem { get; set; } = string.Empty;  
        public string cat62_id { get; set; } = string.Empty;
        public string descripcionAdicional { get; set; } = string.Empty;
        public decimal cantidad { get; set; }
        public string cat03_id { get; set; } = string.Empty;
        public string cat03_idAbrev { get; set; } = string.Empty;
        public decimal cantidadReferencial { get; set; }
        public string cat03_idReferencial { get; set; } = string.Empty;
        public decimal gradosAlcohol { get; set; }
        public decimal cantidadMililitros { get; set; }
        public decimal precioUnitarioReferencia { get; set; }
        public DateTime fechaElaboracion { get; set; }
        public DateTime fechaVencimiento { get; set; }
        public decimal precioUnitario { get; set; }
        public decimal montoDescuento { get; set; }
        public decimal montoRecargo { get; set; }
        public string cat02_id { get; set; } = string.Empty;
        public decimal monto { get; set; }
        public bool IndicadorConCodigo { get; set; }
        public decimal itbis { get; set; }
        public string identificadorPromocion { get; set; } = string.Empty;
        public string descripcionPromocion { get; set; } = string.Empty;
    }
}
