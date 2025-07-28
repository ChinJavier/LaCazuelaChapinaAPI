// =============================================
// ARCHIVO: DTOs/Sucursales/SucursalesDto.cs
// DTOs para gestión de sucursales
// =============================================

using System.ComponentModel.DataAnnotations;

namespace LaCazuelaChapina.API.DTOs.Sucursales
{
    /// <summary>
    /// DTO básico para representar una sucursal
    /// </summary>
    public class SucursalDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Direccion { get; set; }
        public string? Telefono { get; set; }
        public string? Email { get; set; }
        public DateTime? FechaApertura { get; set; }
        public bool Activa { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime FechaActualizacion { get; set; }
    }

    /// <summary>
    /// DTO detallado para una sucursal con estadísticas
    /// </summary>
    public class SucursalDetalleDto : SucursalDto
    {
        public int VentasHoy { get; set; }
        public decimal IngresosDiarios { get; set; }
        public int ProductosCriticos { get; set; }
        public DateTime UltimaVenta { get; set; }
        public string EstadoOperativo { get; set; } = "Activa";
    }

    /// <summary>
    /// DTO para crear una nueva sucursal
    /// </summary>
    public class CrearSucursalDto
    {
        [Required(ErrorMessage = "El nombre de la sucursal es obligatorio")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "La dirección no puede exceder 200 caracteres")]
        public string? Direccion { get; set; }

        [Phone(ErrorMessage = "El formato del teléfono no es válido")]
        [StringLength(15, ErrorMessage = "El teléfono no puede exceder 15 caracteres")]
        public string? Telefono { get; set; }

        [EmailAddress(ErrorMessage = "El formato del email no es válido")]
        [StringLength(100, ErrorMessage = "El email no puede exceder 100 caracteres")]
        public string? Email { get; set; }

        public DateTime? FechaApertura { get; set; }
    }

    /// <summary>
    /// DTO para actualizar una sucursal existente
    /// </summary>
    public class ActualizarSucursalDto
    {
        [Required(ErrorMessage = "El nombre de la sucursal es obligatorio")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "La dirección no puede exceder 200 caracteres")]
        public string? Direccion { get; set; }

        [Phone(ErrorMessage = "El formato del teléfono no es válido")]
        [StringLength(15, ErrorMessage = "El teléfono no puede exceder 15 caracteres")]
        public string? Telefono { get; set; }

        [EmailAddress(ErrorMessage = "El formato del email no es válido")]
        [StringLength(100, ErrorMessage = "El email no puede exceder 100 caracteres")]
        public string? Email { get; set; }

        public DateTime? FechaApertura { get; set; }
        public bool Activa { get; set; } = true;
    }

    // =============================================
    // REPORTES Y ESTADÍSTICAS
    // =============================================

    /// <summary>
    /// DTO para el reporte de ventas de una sucursal
    /// </summary>
    public class ReporteVentasSucursalDto
    {
        public int SucursalId { get; set; }
        public string SucursalNombre { get; set; } = string.Empty;
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public int TotalVentas { get; set; }
        public decimal MontoTotal { get; set; }
        public decimal TicketPromedio { get; set; }
        public List<VentaDiariaDto> VentasPorDia { get; set; } = new();
        public List<ProductoVendidoSucursalDto> ProductosMasVendidos { get; set; } = new();
    }

    /// <summary>
    /// DTO para ventas por día
    /// </summary>
    public class VentaDiariaDto
    {
        public DateTime Fecha { get; set; }
        public int CantidadVentas { get; set; }
        public decimal MontoTotal { get; set; }
        public decimal TicketPromedio { get; set; }
        public string DiaSemana => Fecha.ToString("dddd", new System.Globalization.CultureInfo("es-ES"));
    }

    /// <summary>
    /// DTO para productos más vendidos por sucursal
    /// </summary>
    public class ProductoVendidoSucursalDto
    {
        public string Nombre { get; set; } = string.Empty;
        public int CantidadVendida { get; set; }
        public decimal MontoTotal { get; set; }
        public decimal PrecioPromedio => CantidadVendida > 0 ? MontoTotal / CantidadVendida : 0;
    }

    // =============================================
    // INVENTARIO POR SUCURSAL
    // =============================================

    /// <summary>
    /// DTO para el estado del inventario de una sucursal
    /// </summary>
    public class EstadoInventarioSucursalDto
    {
        public int SucursalId { get; set; }
        public string SucursalNombre { get; set; } = string.Empty;
        public int TotalProductos { get; set; }
        public int ProductosCriticos { get; set; }
        public int ProductosAgotados { get; set; }
        public decimal ValorTotalInventario { get; set; }
        public List<CategoriStockDto> ProductosPorCategoria { get; set; } = new();
        public List<ProductoCriticoDto> ProductosCriticosDetalle { get; set; } = new();
    }

    /// <summary>
    /// DTO para el stock por categoría
    /// </summary>
    public class CategoriStockDto
    {
        public string Categoria { get; set; } = string.Empty;
        public int CantidadProductos { get; set; }
        public int ProductosCriticos { get; set; }
        public decimal ValorInventario { get; set; }
        public double PorcentajeCriticos => CantidadProductos > 0 ? 
            (ProductosCriticos * 100.0) / CantidadProductos : 0;
    }

    /// <summary>
    /// DTO para productos en estado crítico
    /// </summary>
    public class ProductoCriticoDto
    {
        public string Nombre { get; set; } = string.Empty;
        public decimal CantidadActual { get; set; }
        public decimal StockMinimo { get; set; }
        public string UnidadMedida { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public double PorcentajeStock => StockMinimo > 0 ? 
            (double)(CantidadActual / StockMinimo) * 100 : 0;
        public string NivelCriticidad => PorcentajeStock switch
        {
            <= 0 => "AGOTADO",
            <= 50 => "CRÍTICO",
            <= 100 => "BAJO",
            _ => "NORMAL"
        };
    }

    // =============================================
    // ESTADO GENERAL DEL SISTEMA
    // =============================================

    /// <summary>
    /// DTO para el estado general de todas las sucursales
    /// </summary>
    public class EstadoGeneralSucursalesDto
    {
        public int TotalSucursales { get; set; }
        public int SucursalesActivas { get; set; }
        public int VentasTotalesHoy { get; set; }
        public decimal IngresosTotalesHoy { get; set; }
        public int SucursalesConAlertas { get; set; }
        public List<ResumenSucursalDto> ResumenPorSucursal { get; set; } = new();
        public decimal PromedioVentasPorSucursal => SucursalesActivas > 0 ? 
            (decimal)VentasTotalesHoy / SucursalesActivas : 0;
        public decimal PromedioIngresosPorSucursal => SucursalesActivas > 0 ? 
            IngresosTotalesHoy / SucursalesActivas : 0;
    }

    /// <summary>
    /// DTO para el resumen de una sucursal en el dashboard general
    /// </summary>
    public class ResumenSucursalDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int VentasHoy { get; set; }
        public decimal IngresosHoy { get; set; }
        public int ProductosCriticos { get; set; }
        public DateTime UltimaActualizacion { get; set; }
        public string EstadoOperativo => ProductosCriticos > 5 ? "ALERTA" : 
                                       ProductosCriticos > 0 ? "PRECAUCION" : "NORMAL";
        public bool TieneAlertas => ProductosCriticos > 0;
    }
}