namespace Validar.XML.Proveedor.Models
{
    public class ResultadoConsultaDGII
    {
        public bool Exitoso { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
        public string ContenidoRespuesta { get; set; } = string.Empty;
    }
}
