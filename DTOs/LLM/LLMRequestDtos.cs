// =============================================
// ARCHIVO: DTOs/LLM/LLMRequestDtos.cs
// DTOs para funcionalidades de IA/LLM
// =============================================

using System.ComponentModel.DataAnnotations;

namespace LaCazuelaChapina.API.DTOs.LLM
{
    // =============================================
    // ASISTENTE INTELIGENTE DE PEDIDOS
    // =============================================

    /// <summary>
    /// DTO para solicitar asistencia de IA en la creación de pedidos
    /// </summary>
    public class AsistentePedidoRequestDto
    {
        [Required(ErrorMessage = "La solicitud del cliente es obligatoria")]
        [StringLength(1000, ErrorMessage = "La solicitud no puede exceder 1000 caracteres")]
        public string SolicitudCliente { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue, ErrorMessage = "El presupuesto debe ser mayor a 0")]
        public decimal? PresupuestoMaximo { get; set; }

        [Range(1, 50, ErrorMessage = "El número de personas debe estar entre 1 y 50")]
        public int? NumeroPersonas { get; set; }

        [StringLength(200, ErrorMessage = "La ocasión especial no puede exceder 200 caracteres")]
        public string? OcasionEspecial { get; set; }

        [StringLength(500, ErrorMessage = "Las restricciones no pueden exceder 500 caracteres")]
        public string? Restricciones { get; set; }

        [StringLength(50, ErrorMessage = "El nivel de picante no puede exceder 50 caracteres")]
        public string? NivelPicantePreferido { get; set; }

        public int? SucursalId { get; set; }
    }

    /// <summary>
    /// DTO de respuesta del asistente de pedidos
    /// </summary>
    public class AsistentePedidoResponseDto
    {
        public List<RecomendacionProductoDto> Recomendaciones { get; set; } = new();
        public double TotalAproximado { get; set; }
        public string MensajePersonalizado { get; set; } = string.Empty;
        public List<string> ConsejosAdicionales { get; set; } = new();
        public DateTime FechaRecomendacion { get; set; } = DateTime.UtcNow;
        public string ConfianzaRecomendacion { get; set; } = "Alta";
    }

    /// <summary>
    /// DTO para recomendación individual de producto
    /// </summary>
    public class RecomendacionProductoDto
    {
        public string Tipo { get; set; } = string.Empty; // "producto" | "combo"
        public string Nombre { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public List<string> Personalizaciones { get; set; } = new();
        public double PrecioAproximado { get; set; }
        public string Razon { get; set; } = string.Empty;
        public int? ProductoId { get; set; }
        public int? ComboId { get; set; }
    }

    // =============================================
    // ANÁLISIS INTELIGENTE DE VENTAS
    // =============================================

    /// <summary>
    /// DTO para solicitar análisis inteligente de ventas
    /// </summary>
    public class AnalisisVentasRequestDto
    {
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public int? SucursalId { get; set; }

        [Required(ErrorMessage = "El tipo de análisis es obligatorio")]
        public string TipoAnalisis { get; set; } = "general"; // "general", "productos", "tendencias", "rentabilidad"

        [StringLength(200, ErrorMessage = "El enfoque especial no puede exceder 200 caracteres")]
        public string? EnfoqueEspecial { get; set; }
    }

    /// <summary>
    /// DTO de respuesta del análisis de ventas con IA
    /// </summary>
    public class AnalisisVentasLLMDto
    {
        public string ResumenEjecutivo { get; set; } = string.Empty;
        public List<string> TendenciasPrincipales { get; set; } = new();
        public List<ProductoEstrella> ProductosEstrella { get; set; } = new();
        public List<string> OportunidadesMejora { get; set; } = new();
        public List<EstrategiaSugerida> EstrategiasSugeridas { get; set; } = new();
        public List<string> Predicciones { get; set; } = new();
        public List<string> Alertas { get; set; } = new();
        public DateTime FechaAnalisis { get; set; }
        public string PeriodoAnalizado { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO para producto estrella en el análisis
    /// </summary>
    public class ProductoEstrella
    {
        public string Nombre { get; set; } = string.Empty;
        public string Rendimiento { get; set; } = string.Empty;
        public string Recomendacion { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO para estrategia sugerida
    /// </summary>
    public class EstrategiaSugerida
    {
        public string Estrategia { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string ImpactoEsperado { get; set; } = string.Empty;
        public string DificultadImplementacion { get; set; } = string.Empty;
    }

    // =============================================
    // GENERADOR DE DESCRIPCIONES CREATIVAS
    // =============================================

    /// <summary>
    /// DTO para generar descripciones creativas de productos
    /// </summary>
    public class GenerarDescripcionRequestDto
    {
        [Required(ErrorMessage = "El nombre del producto es obligatorio")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        public string NombreProducto { get; set; } = string.Empty;

        [Required(ErrorMessage = "El tipo de producto es obligatorio")]
        public string TipoProducto { get; set; } = string.Empty; // "tamal", "bebida", "combo"

        public List<string>? Ingredientes { get; set; }

        [StringLength(200, ErrorMessage = "La ocasión especial no puede exceder 200 caracteres")]
        public string? OcasionEspecial { get; set; }

        [StringLength(50, ErrorMessage = "El tono no puede exceder 50 caracteres")]
        public string? TonoDescripcion { get; set; } // "tradicional", "moderno", "festivo", "elegante"
    }

    /// <summary>
    /// DTO de respuesta para descripciones creativas
    /// </summary>
    public class DescripcionCreativaDto
    {
        public string DescripcionPrincipal { get; set; } = string.Empty;
        public string DescripcionExtendida { get; set; } = string.Empty;
        public string SloganSugerido { get; set; } = string.Empty;
        public List<string> HashtagsSugeridos { get; set; } = new();
        public List<string> MomentosIdeales { get; set; } = new();
        public List<string> MaridajesSugeridos { get; set; } = new();
        public DateTime FechaGeneracion { get; set; } = DateTime.UtcNow;
    }

    // =============================================
    // CHATBOT INTELIGENTE
    // =============================================

    /// <summary>
    /// DTO para consulta al chatbot inteligente
    /// </summary>
    public class ChatbotRequestDto
    {
        [Required(ErrorMessage = "La pregunta es obligatoria")]
        [StringLength(1000, ErrorMessage = "La pregunta no puede exceder 1000 caracteres")]
        public string Pregunta { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "El ID de sesión no puede exceder 100 caracteres")]
        public string SesionId { get; set; } = Guid.NewGuid().ToString();

        public List<string>? HistorialConversacion { get; set; }

        public int? SucursalId { get; set; }

        [StringLength(50, ErrorMessage = "El idioma no puede exceder 50 caracteres")]
        public string Idioma { get; set; } = "es"; // "es", "en"
    }

    /// <summary>
    /// DTO de respuesta del chatbot
    /// </summary>
    public class ChatbotResponseDto
    {
        public string Respuesta { get; set; } = string.Empty;
        public List<string> ProductosSugeridos { get; set; } = new();
        public List<string> AccionesSugeridas { get; set; } = new();
        public bool NecesitaHumano { get; set; }
        public string CategoriaConsulta { get; set; } = string.Empty;
        public string ConfianzaRespuesta { get; set; } = string.Empty;
        public DateTime FechaRespuesta { get; set; } = DateTime.UtcNow;
        public string SesionId { get; set; } = string.Empty;
    }

    // =============================================
    // OPTIMIZACIÓN DE INVENTARIO
    // =============================================

    /// <summary>
    /// DTO para solicitar optimización de inventario con IA
    /// </summary>
    public class OptimizarInventarioRequestDto
    {
        public int? SucursalId { get; set; }

        [Range(1, 365, ErrorMessage = "Los días de proyección deben estar entre 1 y 365")]
        public int? DiasProyeccion { get; set; } = 30;

        [Range(50, 99, ErrorMessage = "El nivel de servicio debe estar entre 50% y 99%")]
        public int? NivelServicioDeseado { get; set; } = 95;

        public bool ConsiderarEstacionalidad { get; set; } = true;

        [StringLength(200, ErrorMessage = "Los parámetros adicionales no pueden exceder 200 caracteres")]
        public string? ParametrosAdicionales { get; set; }
    }

    /// <summary>
    /// DTO de respuesta para optimización de inventario
    /// </summary>
    public class OptimizacionInventarioDto
    {
        public string ResumenGeneral { get; set; } = string.Empty;
        public List<ProductoCriticoInventario> ProductosCriticos { get; set; } = new();
        public List<string> OportunidadesAhorro { get; set; } = new();
        public List<string> AlertasEstacionales { get; set; } = new();
        public List<RecomendacionCompra> RecomendacionesCompra { get; set; } = new();
        public double AhorroEstimado { get; set; }
        public string RiesgoDesabasto { get; set; } = string.Empty;
        public DateTime FechaAnalisis { get; set; }
        public int? SucursalAnalizada { get; set; }
    }

    /// <summary>
    /// DTO para producto crítico en inventario
    /// </summary>
    public class ProductoCriticoInventario
    {
        public string Nombre { get; set; } = string.Empty;
        public double StockActual { get; set; }
        public string ProyeccionAgotamiento { get; set; } = string.Empty;
        public double CantidadSugeridaCompra { get; set; }
        public string Prioridad { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO para recomendación de compra
    /// </summary>
    public class RecomendacionCompra
    {
        public string Producto { get; set; } = string.Empty;
        public double Cantidad { get; set; }
        public string Justificacion { get; set; } = string.Empty;
    }

    // =============================================
    // PROCESAMIENTO DE VOZ (BONUS)
    // =============================================

    /// <summary>
    /// DTO para procesar audio de voz a texto
    /// </summary>
    public class ProcesarVozRequestDto
    {
        [Required(ErrorMessage = "El archivo de audio es obligatorio")]
        public IFormFile AudioFile { get; set; } = null!;

        [StringLength(10, ErrorMessage = "El idioma no puede exceder 10 caracteres")]
        public string Idioma { get; set; } = "es-GT"; // Español de Guatemala

        public int? SucursalId { get; set; }

        [StringLength(100, ErrorMessage = "El ID de sesión no puede exceder 100 caracteres")]
        public string? SesionId { get; set; }
    }

    /// <summary>
    /// DTO de respuesta para procesamiento de voz
    /// </summary>
    public class ProcesarVozResponseDto
    {
        public string TextoDetectado { get; set; } = string.Empty;
        public float ConfianzaDeteccion { get; set; }
        public AsistentePedidoResponseDto? PedidoInterpretado { get; set; }
        public bool RequiereConfirmacion { get; set; }
        public List<string> AlternativasTexto { get; set; } = new();
        public string IdimaDetectado { get; set; } = string.Empty;
        public DateTime FechaProcesamiento { get; set; } = DateTime.UtcNow;
    }

    // =============================================
    // ANÁLISIS AVANZADO Y PREDICCIONES
    // =============================================

    /// <summary>
    /// DTO para análisis predictivo de demanda
    /// </summary>
    public class AnalisisPredictivoRequestDto
    {
        public int? SucursalId { get; set; }

        [Required(ErrorMessage = "El tipo de predicción es obligatorio")]
        public string TipoPrediccion { get; set; } = string.Empty; // "demanda", "inventario", "ventas", "estacional"

        [Range(1, 90, ErrorMessage = "Los días a predecir deben estar entre 1 y 90")]
        public int DiasAPredecir { get; set; } = 7;

        public List<string>? FactoresConsiderar { get; set; } // "clima", "festividades", "promociones"

        [StringLength(500, ErrorMessage = "El contexto adicional no puede exceder 500 caracteres")]
        public string? ContextoAdicional { get; set; }
    }

    /// <summary>
    /// DTO de respuesta para análisis predictivo
    /// </summary>
    public class AnalisisPredictivoResponseDto
    {
        public string TipoAnalisis { get; set; } = string.Empty;
        public List<PrediccionDemanda> Predicciones { get; set; } = new();
        public List<string> FactoresInfluyentes { get; set; } = new();
        public List<RecomendacionEstrategica> RecomendacionesEstrategicas { get; set; } = new();
        public string ConfianzaPrediccion { get; set; } = string.Empty;
        public List<string> AlertasPreventivas { get; set; } = new();
        public DateTime FechaAnalisis { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// DTO para predicción de demanda individual
    /// </summary>
    public class PrediccionDemanda
    {
        public DateTime Fecha { get; set; }
        public string Producto { get; set; } = string.Empty;
        public double DemandaEsperada { get; set; }
        public double ConfianzaPrediccion { get; set; }
        public List<string> FactoresInfluyentes { get; set; } = new();
    }

    /// <summary>
    /// DTO para recomendación estratégica
    /// </summary>
    public class RecomendacionEstrategica
    {
        public string Titulo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty; // "inventario", "marketing", "operaciones"
        public string Prioridad { get; set; } = string.Empty;
        public string ImpactoEsperado { get; set; } = string.Empty;
        public List<string> PasosImplementacion { get; set; } = new();
    }

    // =============================================
    // CONFIGURACIÓN Y PERSONALIZACIÓN
    // =============================================

    /// <summary>
    /// DTO para configurar el comportamiento del LLM
    /// </summary>
    public class ConfiguracionLLMDto
    {
        public string ModeloPreferido { get; set; } = "meta-llama/llama-3.2-3b-instruct:free";
        public float Temperatura { get; set; } = 0.7f;
        public int MaxTokens { get; set; } = 1000;
        public string PersonalidadChatbot { get; set; } = "amigable";
        public List<string> PalabrasClavePersonalizadas { get; set; } = new();
        public bool UsarContextoHistorico { get; set; } = true;
        public string IdiomaPreferido { get; set; } = "es";
    }

    /// <summary>
    /// DTO para métricas de uso del LLM
    /// </summary>
    public class MetricasLLMDto
    {
        public int ConsultasTotales { get; set; }
        public int ConsultasExitosas { get; set; }
        public double TiempoPromedioRespuesta { get; set; }
        public Dictionary<string, int> ConsultasPorTipo { get; set; } = new();
        public List<string> ErroresFrecuentes { get; set; } = new();
        public double SatisfaccionPromedio { get; set; }
        public DateTime PeriodoAnalisis { get; set; }
    }
}