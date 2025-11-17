namespace Validar.XML.Proveedor.Models
{
    public class MensajeValidacion
    {
        public string IdProceso { get; set; } = string.Empty;
        public string BlobXml { get; set; } = string.Empty;
        public string BlobPdf { get; set; } = string.Empty;
        public string EmisorId { get; set; } = string.Empty;
        public DateTime FechaEnvio { get; set; }
    }
}
