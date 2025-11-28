using _proveedor = Validar.XML.Proveedor.Models.Proveedor;

namespace Validar.XML.Proveedor.Services
{
    public interface IProveedoresService
    {
        Task<bool> GuardarProveedores(List<_proveedor> proveedores, int accion);
    }
}
