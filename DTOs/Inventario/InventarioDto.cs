using LaCazuelaChapina.API.Models.Enums;

namespace LaCazuelaChapina.API.DTOs.Inventario
{
    public class StockSucursalDto
    {
        public int Id { get; set; }
        public string SucursalNombre { get; set; } = string.Empty;
        public string MateriaPrimaNombre { get; set; } = string.Empty;
        public string CategoriaNombre { get; set; } = string.Empty;
        public decimal CantidadActual { get; set; }
        public string UnidadMedida { get; set; } = string.Empty;
        public decimal StockMinimo { get; set; }
        public decimal StockMaximo { get; set; }
        public decimal CostoPromedio { get; set; }
        public string EstadoStock { get; set; } = string.Empty;
        public decimal PorcentajeStock { get; set; }
        public decimal ValorInventario { get; set; }
        public DateTime FechaUltimaActualizacion { get; set; }
        public bool RequiereReorden { get; set; }
    }

    public class AlertaStockDto
    {
        public int MateriaPrimaId { get; set; }
        public string MateriaPrimaNombre { get; set; } = string.Empty;
        public string CategoriaNombre { get; set; } = string.Empty;
        public decimal CantidadActual { get; set; }
        public decimal StockMinimo { get; set; }
        public string UnidadMedida { get; set; } = string.Empty;
        public decimal PorcentajeStock { get; set; }
        public string TipoAlerta { get; set; } = string.Empty; // AGOTADO, STOCK_BAJO, CRITICO
        public string NivelPrioridad { get; set; } = string.Empty; // ALTA, MEDIA, BAJA
        public DateTime FechaDeteccion { get; set; }
        public int DiasEstimadosAgotamiento { get; set; }
        public decimal CostoReposicion { get; set; }
        public List<string> ProductosAfectados { get; set; } = new();
    }

    public class MovimientoInventarioDto
    {
        public int Id { get; set; }
        public string SucursalNombre { get; set; } = string.Empty;
        public string MateriaPrimaNombre { get; set; } = string.Empty;
        public string CategoriaNombre { get; set; } = string.Empty;
        public TipoMovimiento TipoMovimiento { get; set; }
        public decimal Cantidad { get; set; }
        public string UnidadMedida { get; set; } = string.Empty;
        public decimal? CostoUnitario { get; set; }
        public decimal? MontoTotal { get; set; }
        public string? Motivo { get; set; }
        public string? DocumentoReferencia { get; set; }
        public DateTime FechaMovimiento { get; set; }
        public decimal StockAnterior { get; set; }
        public decimal StockActual { get; set; }
    }

    public class RegistrarEntradaDto
    {
        public int SucursalId { get; set; }
        public int MateriaPrimaId { get; set; }
        public decimal Cantidad { get; set; }
        public decimal CostoUnitario { get; set; }
        public string Motivo { get; set; } = string.Empty;
        public string? DocumentoReferencia { get; set; }
        public string? Proveedor { get; set; }
        public DateTime? FechaVencimiento { get; set; }
        public string? Lote { get; set; }
    }

    public class RegistrarSalidaDto
    {
        public int SucursalId { get; set; }
        public int MateriaPrimaId { get; set; }
        public decimal Cantidad { get; set; }
        public string Motivo { get; set; } = string.Empty;
        public string? DocumentoReferencia { get; set; }
        public int? VentaId { get; set; } // Si es por venta
        public List<int>? ProductosRelacionados { get; set; }
    }

    public class RegistrarMermaDto
    {
        public int SucursalId { get; set; }
        public int MateriaPrimaId { get; set; }
        public decimal Cantidad { get; set; }
        public string Motivo { get; set; } = string.Empty;
        public string TipoMerma { get; set; } = string.Empty; // vencimiento, daño, contaminación, derrame
        public string? Observaciones { get; set; }
        public bool RequiereInvestigacion { get; set; } = false;
    }

    public class RegistrarAjusteDto
    {
        public int SucursalId { get; set; }
        public int MateriaPrimaId { get; set; }
        public decimal CantidadAnterior { get; set; }
        public decimal CantidadNueva { get; set; }
        public string Motivo { get; set; } = string.Empty;
        public string? Observaciones { get; set; }
        public string ResponsableAjuste { get; set; } = string.Empty;
    }

    public class MateriaPrimaDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string CategoriaNombre { get; set; } = string.Empty;
        public string UnidadMedida { get; set; } = string.Empty;
        public decimal StockMinimo { get; set; }
        public decimal StockMaximo { get; set; }
        public decimal CostoPromedio { get; set; }
        public bool Activa { get; set; }
        public List<StockSucursalDto> StocksPorSucursal { get; set; } = new();
    }

    public class ResumenInventarioDto
    {
        public string SucursalNombre { get; set; } = string.Empty;
        public int TotalMateriasPrimas { get; set; }
        public decimal ValorTotalInventario { get; set; }
        public int MaterialesStockBajo { get; set; }
        public int MaterialesAgotados { get; set; }
        public int MovimientosHoy { get; set; }
        public decimal MontoMermasDelMes { get; set; }
        public List<AlertaStockDto> AlertasPrioritarias { get; set; } = new();
        public List<MaterialMasConsumidoDto> MaterialesMasConsumidos { get; set; } = new();
    }

    public class MaterialMasConsumidoDto
    {
        public string MateriaPrimaNombre { get; set; } = string.Empty;
        public decimal CantidadConsumida { get; set; }
        public string UnidadMedida { get; set; } = string.Empty;
        public decimal CostoTotal { get; set; }
        public decimal PorcentajeDelTotal { get; set; }
    }

    public class ProyeccionStockDto
    {
        public int MateriaPrimaId { get; set; }
        public string MateriaPrimaNombre { get; set; } = string.Empty;
        public decimal StockActual { get; set; }
        public decimal ConsumoPromedioDiario { get; set; }
        public int DiasRestantes { get; set; }
        public DateTime FechaAgotamientoEstimada { get; set; }
        public decimal CantidadRecomendadaCompra { get; set; }
        public string NivelRiesgo { get; set; } = string.Empty; // ALTO, MEDIO, BAJO
        public List<string> RecomendacionesAccion { get; set; } = new();
    }

    public class ReporteInventarioDto
    {
        public DateTime FechaGeneracion { get; set; }
        public string SucursalNombre { get; set; } = string.Empty;
        public string PeriodoAnalisis { get; set; } = string.Empty;
        public ResumenInventarioDto Resumen { get; set; } = new();
        public List<MovimientoInventarioDto> UltimosMovimientos { get; set; } = new();
        public List<ProyeccionStockDto> ProyeccionesStock { get; set; } = new();
        public List<RecomendacionCompraDto> RecomendacionesCompra { get; set; } = new();
    }

    public class RecomendacionCompraDto
    {
        public int MateriaPrimaId { get; set; }
        public string MateriaPrimaNombre { get; set; } = string.Empty;
        public decimal CantidadRecomendada { get; set; }
        public string UnidadMedida { get; set; } = string.Empty;
        public decimal CostoEstimado { get; set; }
        public string Prioridad { get; set; } = string.Empty;
        public string Justificacion { get; set; } = string.Empty;
        public DateTime FechaSugeridaCompra { get; set; }
    }

    // DTOs para filtros y búsquedas
    public class FiltroInventarioDto
    {
        public int? SucursalId { get; set; }
        public int? CategoriaId { get; set; }
        public string? EstadoStock { get; set; } // AGOTADO, STOCK_BAJO, OK
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
        public TipoMovimiento? TipoMovimiento { get; set; }
        public string? TextoBusqueda { get; set; }
        public bool SoloAlertas { get; set; } = false;
        public string? OrdenarPor { get; set; } // nombre, cantidad, valor, fecha
        public bool OrdenDescendente { get; set; } = true;
    }
}