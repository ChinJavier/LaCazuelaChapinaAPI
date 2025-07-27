using LaCazuelaChapina.API.Models.Enums;

namespace LaCazuelaChapina.API.DTOs.Dashboard
{
    public class DashboardDto
    {
        public decimal VentasDiarias { get; set; }
        public decimal VentasMensuales { get; set; }
        public int TransaccionesDiarias { get; set; }
        public int TransaccionesMensuales { get; set; }
        public decimal TicketPromedio { get; set; }
        public List<ProductoVendidoDto> TamalesMasVendidos { get; set; } = new();
        public List<BebidaPorHorarioDto> BebidasPorHorario { get; set; } = new();
        public ProporcionPicanteDto ProporcionPicante { get; set; } = new();
        public List<UtilidadLineaDto> UtilidadesPorLinea { get; set; } = new();
        public List<DesperdicioDto> DesperdicioMateriasPrimas { get; set; } = new();
        public MetricasInventarioDto MetricasInventario { get; set; } = new();
        public List<VentaPorDiaDto> VentasUltimos7Dias { get; set; } = new();
        public DateTime FechaGeneracion { get; set; } = DateTime.UtcNow;
    }

    public class ProductoVendidoDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public int CantidadVendida { get; set; }
        public decimal MontoTotal { get; set; }
        public decimal PorcentajeDelTotal { get; set; }
        public string VarianteMasVendida { get; set; } = string.Empty;
    }

    public class BebidaPorHorarioDto
    {
        public string TipoBebida { get; set; } = string.Empty;
        public int Hora { get; set; }
        public int CantidadVendida { get; set; }
        public decimal MontoVendido { get; set; }
        public string PeriodoNombre { get; set; } = string.Empty; // "Mañana", "Tarde", "Noche"
    }

    public class ProporcionPicanteDto
    {
        public int TotalConPicante { get; set; }
        public int TotalSinPicante { get; set; }
        public decimal PorcentajeConPicante { get; set; }
        public List<DetallePicanteDto> DetallePorNivel { get; set; } = new();
    }

    public class DetallePicanteDto
    {
        public string NivelPicante { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal Porcentaje { get; set; }
    }

    public class UtilidadLineaDto
    {
        public string Linea { get; set; } = string.Empty;
        public decimal Ingresos { get; set; }
        public decimal CostosEstimados { get; set; }
        public decimal UtilidadEstimada { get; set; }
        public decimal MargenPorcentaje { get; set; }
        public int CantidadProductos { get; set; }
        public decimal TicketPromedio { get; set; }
    }

    public class DesperdicioDto
    {
        public string MateriaPrima { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public decimal CantidadDesperdiciada { get; set; }
        public decimal CostoDelDesperdicio { get; set; }
        public string UnidadMedida { get; set; } = string.Empty;
        public decimal PorcentajeDelTotal { get; set; }
        public string MotivosPrincipales { get; set; } = string.Empty;
    }

    public class MetricasInventarioDto
    {
        public decimal ValorTotalInventario { get; set; }
        public int MaterialesStockBajo { get; set; }
        public int MaterialesAgotados { get; set; }
        public int TotalMovimientosHoy { get; set; }
        public decimal MontoComprasDelMes { get; set; }
        public decimal MontoMermasDelMes { get; set; }
        public List<AlertaStockResumenDto> AlertasPrioritarias { get; set; } = new();
    }

    public class AlertaStockResumenDto
    {
        public string MateriaPrima { get; set; } = string.Empty;
        public string TipoAlerta { get; set; } = string.Empty;
        public decimal CantidadActual { get; set; }
        public string UnidadMedida { get; set; } = string.Empty;
        public int DiasEstimadosAgotamiento { get; set; }
    }

    public class VentaPorDiaDto
    {
        public DateTime Fecha { get; set; }
        public string DiaNombre { get; set; } = string.Empty;
        public decimal MontoVentas { get; set; }
        public int CantidadTransacciones { get; set; }
        public decimal TicketPromedio { get; set; }
        public bool EsFestivo { get; set; }
    }

    public class ResumenComparativoDto
    {
        public string Periodo { get; set; } = string.Empty;
        public decimal VentasActual { get; set; }
        public decimal VentasAnterior { get; set; }
        public decimal CrecimientoPorcentaje { get; set; }
        public string Tendencia { get; set; } = string.Empty; // "SUBIENDO", "BAJANDO", "ESTABLE"
    }

    public class ProductividadDto
    {
        public int TamalesProducidosHoy { get; set; }
        public int BebidasPreparadasHoy { get; set; }
        public decimal TiempoPromedioPreparacion { get; set; }
        public decimal CapacidadUtilizada { get; set; }
        public int PedidosPendientes { get; set; }
    }

    public class ClienteDto
    {
        public string TipoCliente { get; set; } = string.Empty; // "Frecuente", "Nuevo", "Ocasional"
        public int CantidadClientes { get; set; }
        public decimal MontoPromedioPorCliente { get; set; }
        public decimal PorcentajeDelTotal { get; set; }
    }

    public class MetricasTemporalesDto
    {
        public string Periodo { get; set; } = string.Empty; // "Hora", "Día", "Semana", "Mes"
        public List<PuntoTemporalDto> Datos { get; set; } = new();
    }

    public class PuntoTemporalDto
    {
        public DateTime Momento { get; set; }
        public string Etiqueta { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public string Metrica { get; set; } = string.Empty;
    }

    // DTOs para filtros del dashboard
    public class FiltroDashboardDto
    {
        public int SucursalId { get; set; }
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
        public string? TipoMetrica { get; set; } // "ventas", "inventario", "productos"
        public bool IncluirComparacion { get; set; } = true;
        public string? Granularidad { get; set; } = "dia"; // "hora", "dia", "semana", "mes"
    }

    // DTO para el dashboard completo personalizable
    public class DashboardPersonalizadoDto
    {
        public string NombreSucursal { get; set; } = string.Empty;
        public DateTime FechaGeneracion { get; set; }
        public string PeriodoAnalisis { get; set; } = string.Empty;
        
        // Métricas principales
        public Dictionary<string, decimal> MetricasPrincipales { get; set; } = new();
        
        // Gráficos y visualizaciones
        public Dictionary<string, object> Graficos { get; set; } = new();
        
        // Alertas y notificaciones
        public List<AlertaDto> Alertas { get; set; } = new();
        
        // Recomendaciones
        public List<RecomendacionDto> Recomendaciones { get; set; } = new();
    }

    public class AlertaDto
    {
        public string Tipo { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
        public string Prioridad { get; set; } = string.Empty;
        public DateTime FechaGeneracion { get; set; }
        public string AccionRecomendada { get; set; } = string.Empty;
    }

    public class RecomendacionDto
    {
        public string Categoria { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public decimal ImpactoEstimado { get; set; }
        public string Dificultad { get; set; } = string.Empty; // "Fácil", "Media", "Difícil"
        public List<string> PasosAccion { get; set; } = new();
    }
}