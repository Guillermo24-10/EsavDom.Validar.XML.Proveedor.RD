namespace Validar.XML.Proveedor.Models
{
    public class ResultadoValidacion
    {
        public bool EsValido { get; set; }
        public List<string> Errores { get; set; } = new();
        public Dictionary<string, string> Detalles { get; set; } = new();
    }
}
