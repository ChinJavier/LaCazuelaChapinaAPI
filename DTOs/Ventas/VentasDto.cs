using LaCazuelaChapina.API.Models.Enums;
using LaCazuelaChapina.API.DTOs.Personalizacion;

namespace LaCazuelaChapina.API.DTOs.Ventas
{
    public class CrearVentaDto
    {
        public int SucursalId { get; set; }
        public string? ClienteNombre { get; set; }
        public string? ClienteTelefono { get; set; }
        public TipoPago TipoPago { get; set; }
        public List<DetalleVentaDto> Detalles { get; set; } = new();
    }

    public class DetalleVentaDto
    {
        public int? ProductoId { get; set; }
        public int? VarianteProductoId { get; set; }
        public int? ComboId { get; set; }
        public int Cantidad { get; set; }
        public string? Notas { get; set; }
        public List<PersonalizacionDto> Personalizaciones { get; set; } = new();
    }

    public class VentaDto
    {
        public int Id { get; set; }
        public string NumeroVenta { get; set; } = string.Empty;
        public DateTime FechaVenta { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Descuento { get; set; }
        public decimal Total { get; set; }
        public TipoPago TipoPago { get; set; }
        public EstadoVenta EstadoVenta { get; set; }
        public string? ClienteNombre { get; set; }
        public string? ClienteTelefono { get; set; }
        public string SucursalNombre { get; set; } = string.Empty;
        public List<DetalleVentaResponseDto> Detalles { get; set; } = new();
    }

    public class DetalleVentaResponseDto
    {
        public int Id { get; set; }
        public string ProductoNombre { get; set; } = string.Empty;
        public string? VarianteNombre { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal { get; set; }
        public string? Notas { get; set; }
        public List<PersonalizacionResponseDto> Personalizaciones { get; set; } = new();
    }

    public class PersonalizacionResponseDto
    {
        public int Id { get; set; }
        public string TipoAtributoNombre { get; set; } = string.Empty;
        public string OpcionNombre { get; set; } = string.Empty;
        public decimal PrecioAdicional { get; set; }
    }

    public class VentaResumenDto
    {
        public int Id { get; set; }
        public string NumeroVenta { get; set; } = string.Empty;
        public DateTime FechaVenta { get; set; }
        public decimal Total { get; set; }
        public TipoPago TipoPago { get; set; }
        public EstadoVenta EstadoVenta { get; set; }
        public string? ClienteNombre { get; set; }
        public string SucursalNombre { get; set; } = string.Empty;
        public int CantidadItems { get; set; }
    }

    public class VentasPaginadasDto
    {
        public List<VentaResumenDto> Ventas { get; set; } = new();
        public int TotalVentas { get; set; }
        public int PaginaActual { get; set; }
        public int TamanoPagina { get; set; }
        public int TotalPaginas { get; set; }
    }

    public class CancelarVentaDto
    {
        public string Motivo { get; set; } = string.Empty;
    }
}