namespace Validar.XML.Proveedor.Models
{
    public class BancoCajaModel
    {
        public string Banc_Id { get; set; } = string.Empty;
        public string Banc_TipoCuenta { get; set; } = string.Empty; 
        public string Banc_Moneda { get; set; } = string.Empty;
        public string Banc_NumeroCuenta { get; set; } = string.Empty;
        public string Banc_NombreCuenta { get; set; } = string.Empty;
        public decimal Banc_SaldoInicial { get; set; } = 0;
        public DateTime Banc_FechaSaldoInicial { get; set; } = DateTime.MinValue;
        public string Banc_Descripcion { get; set; } = string.Empty;    
        public string Banc_IdEmisor { get; set; } = string.Empty;
        public bool Banc_EsCuentaPrincipal { get; set; } = false;
    }
}
