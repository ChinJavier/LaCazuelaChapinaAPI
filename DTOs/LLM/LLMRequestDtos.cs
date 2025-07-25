// =============================================
// ARCHIVO: DTOs/LLM/LLMRequestDtos.cs
// DTOs para solicitudes a servicios LLM
// =============================================

namespace LaCazuelaChapina.API.DTOs.LLM
{
    /// <summary>
    /// Solicitud para generar recomendaciones de combos personalizadas
    /// </summary>
    public class ComboRecommendationRequest
    {
        public string Epoca { get; set; } = string.Empty; // "Navidad", "Cuaresma", "Independencia", etc.
        public decimal PresupuestoMaximo { get; set; }
        public int NumeroPersonas { get; set; }
        public string PreferenciasEspeciales { get; set; } = string.Empty; // "vegetariano", "sin picante", etc.
        public string? OcasionEspecial { get; set; } // "cena familiar", "almuerzo oficina", etc.
        public List<string> RestriccionesAlimentarias { get; set; } = new();
    }

    /// <summary>
    /// Solicitud para análisis de patrones de ventas
    /// </summary>
    public class VentasAnalysisRequest
    {
        public decimal VentasTotalesMes { get; set; }
        public List<string> TamalesMasVendidos { get; set; } = new();
        public List<string> BebidasPorHorario { get; set; } = new();
        public decimal ProporcionPicante { get; set; } // Porcentaje que prefiere picante
        public List<string> DesperdiciosPrincipales { get; set; } = new();
        public string MesActual { get; set; } = string.Empty;
        public string NombreSucursal { get; set; } = string.Empty;
        public int DiasAnalizados { get; set; }
        public Dictionary<string, decimal> VentasPorCategoria { get; set; } = new();
        public List<TendenciaVenta> TendenciasSemanales { get; set; } = new();
    }

    /// <summary>
    /// Solicitud para generar alertas inteligentes de inventario
    /// </summary>
    public class InventoryAlertRequest
    {
        public string MateriaPrima { get; set; } = string.Empty;
        public decimal StockActual { get; set; }
        public decimal StockMinimo { get; set; }
        public string UnidadMedida { get; set; } = string.Empty;
        public int DiasEstimadosAgotamiento { get; set; }
        public string ProveedorPrincipal { get; set; } = string.Empty;
        public string NombreSucursal { get; set; } = string.Empty;
        public List<string> ProductosAfectados { get; set; } = new();
        public decimal DemandaPromediaDiaria { get; set; }
        public decimal CostoPromedio { get; set; }
        public DateTime UltimaCompra { get; set; }
        public bool EsMateriaPrimaCritica { get; set; }
    }

    /// <summary>
    /// Solicitud para generar contenido de marketing
    /// </summary>
    public class MarketingContentRequest
    {
        public string TipoContenido { get; set; } = string.Empty; // "post_facebook", "promocion_whatsapp", "banner_web"
        public string OcasionEspecial { get; set; } = string.Empty; // "Día de la Madre", "Fiesta Patronal"
        public List<string> ProductosDestacados { get; set; } = new();
        public string PublicoObjetivo { get; set; } = string.Empty; // "familias", "oficinistas", "turistas"
        public string TonoDeseado { get; set; } = string.Empty; // "nostálgico", "festivo", "promocional"
        public decimal? DescuentoOfrecido { get; set; }
        public DateTime? FechaVigencia { get; set; }
        public string? CallToAction { get; set; }
    }

    /// <summary>
    /// Solicitud para análisis de competencia
    /// </summary>
    public class CompetitorAnalysisRequest
    {
        public List<CompetitorInfo> Competidores { get; set; } = new();
        public string AreaGeografica { get; set; } = string.Empty;
        public List<ProductInfo> NuestrosProductos { get; set; } = new();
        public decimal NuestroRangoPrecios { get; set; }
        public List<string> VentajasCompetitivas { get; set; } = new();
        public string ObjetivoAnalisis { get; set; } = string.Empty; // "precios", "productos", "marketing"
    }

    /// <summary>
    /// Solicitud para optimización de menú
    /// </summary>
    public class MenuOptimizationRequest
    {
        public List<ProductoVentaData> ProductosActuales { get; set; } = new();
        public decimal CostoPromedioOperacion { get; set; }
        public List<string> IngredientesDisponibles { get; set; } = new();
        public string TemporadaActual { get; set; } = string.Empty;
        public decimal MargenObjetivo { get; set; }
        public List<string> TendenciasGastronomicas { get; set; } = new();
        public int CapacidadProduccionDiaria { get; set; }
    }

    /// <summary>
    /// Solicitud para predicción de demanda
    /// </summary>
    public class DemandForecastRequest
    {
        public List<VentaHistorica> HistorialVentas { get; set; } = new();
        public DateTime FechaPrediccion { get; set; }
        public List<EventoEspecial> EventosProximos { get; set; } = new();
        public DatosClimaticos? PronosticoClima { get; set; }
        public FactoresEstacionales FactoresEstacionales { get; set; } = new();
        public int DiasAdelante { get; set; } = 7; // Por defecto 7 días
    }

    // =============================================
    // CLASES DE APOYO
    // =============================================

    public class TendenciaVenta
    {
        public string Semana { get; set; } = string.Empty;
        public decimal VentasTotal { get; set; }
        public int CantidadTransacciones { get; set; }
        public decimal TicketPromedio { get; set; }
    }

    public class CompetitorInfo
    {
        public string Nombre { get; set; } = string.Empty;
        public string Ubicacion { get; set; } = string.Empty;
        public List<string> ProductosPrincipales { get; set; } = new();
        public decimal RangoPrecios { get; set; }
        public string FortalezaPrincipal { get; set; } = string.Empty;
    }

    public class ProductInfo
    {
        public string Nombre { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public decimal CostoProduccion { get; set; }
        public int VentasPromedioMes { get; set; }
        public decimal MargenActual { get; set; }
    }

    public class ProductoVentaData
    {
        public string Nombre { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public decimal Costo { get; set; }
        public int CantidadVendidaMes { get; set; }
        public decimal TiempoPreparacion { get; set; }
        public List<string> Ingredientes { get; set; } = new();
        public string Categoria { get; set; } = string.Empty;
    }

    public class VentaHistorica
    {
        public DateTime Fecha { get; set; }
        public string Producto { get; set; } = string.Empty;
        public int CantidadVendida { get; set; }
        public decimal MontoVenta { get; set; }
        public string DiaSemana { get; set; } = string.Empty;
        public bool EsDiaFestivo { get; set; }
    }

    public class EventoEspecial
    {
        public string Nombre { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public string TipoEvento { get; set; } = string.Empty; // "religioso", "patrio", "comercial"
        public decimal ImpactoEstimado { get; set; } // Multiplicador de demanda
        public List<string> ProductosRelacionados { get; set; } = new();
    }

    public class DatosClimaticos
    {
        public decimal TemperaturaPromedio { get; set; }
        public bool ProbabilidadLluvia { get; set; }
        public string CondicionGeneral { get; set; } = string.Empty; // "soleado", "nublado", "lluvioso"
    }

    public class FactoresEstacionales
    {
        public string Estacion { get; set; } = string.Empty; // "seca", "lluviosa"
        public string MesActual { get; set; } = string.Empty;
        public bool EsTemporadaAlta { get; set; }
        public List<string> ProductosEstacionales { get; set; } = new();
        public decimal FactorAjuste { get; set; } = 1.0m;
    }

    // =============================================
    // RESPUESTAS ESTRUCTURADAS
    // =============================================

    public class LLMResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public string ModelUsed { get; set; } = string.Empty;
        public double ConfidenceLevel { get; set; }
    }

    public class ComboRecommendationResponse
    {
        public string NombreCombo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public List<ComponenteCombo> Componentes { get; set; } = new();
        public decimal PrecioEstimado { get; set; }
        public string RazonRecomendacion { get; set; } = string.Empty;
        public List<string> BeneficiosDestacados { get; set; } = new();
        public string SugerenciasPresentacion { get; set; } = string.Empty;
    }

    public class ComponenteCombo
    {
        public string Tipo { get; set; } = string.Empty; // "tamal", "bebida"
        public int Cantidad { get; set; }
        public string Especificaciones { get; set; } = string.Empty;
        public decimal PrecioUnitario { get; set; }
        public List<string> PersonalizacionesSugeridas { get; set; } = new();
    }

    public class MarketingContentResponse
    {
        public string TituloContenido { get; set; } = string.Empty;
        public string TextoPrincipal { get; set; } = string.Empty;
        public string CallToAction { get; set; } = string.Empty;
        public List<string> Hashtags { get; set; } = new();
        public List<string> SugerenciasVisuales { get; set; } = new();
        public string TonoEmocional { get; set; } = string.Empty;
        public List<string> FrasesDestacadas { get; set; } = new();
    }
}