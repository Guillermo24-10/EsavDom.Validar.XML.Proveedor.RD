namespace Validar.XML.Proveedor.Models
{
    public class MensajeCola
    {
        public string MessageId { get; set; } = string.Empty;
        public string PopReceipt { get; set; } = string.Empty;
        public string MessageText { get; set; } = string.Empty;
        public int DequeueCount { get; set; }
        public MensajeValidacion Datos { get; set; } = new();
    }
}
