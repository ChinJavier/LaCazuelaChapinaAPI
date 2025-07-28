// =============================================
// ARCHIVO: DTOs/Notificaciones/NotificacionesDto.cs
// DTOs para gestión de notificaciones
// =============================================

using System.ComponentModel.DataAnnotations;
using LaCazuelaChapina.API.Models.Enums;

namespace LaCazuelaChapina.API.DTOs.Notificaciones
{
    /// <summary>
    /// DTO básico para representar una notificación
    /// </summary>
    public class NotificacionDto
    {
        public int Id { get; set; }
        public int SucursalId { get; set; }
        public string SucursalNombre { get; set; } = string.Empty;
        public TipoNotificacion TipoNotificacion { get; set; }
        public string TipoNotificacionTexto => TipoNotificacion.ToString();
        public string Titulo { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaEnvio { get; set; }
        public bool Enviada { get; set; }
        public int? ReferenciaId { get; set; }
        public string TiempoTranscurrido => CalcularTiempoTranscurrido();
        public string EstadoTexto => Enviada ? "Enviada" : "Pendiente";
        public string IconoTipo => TipoNotificacion switch
        {
            TipoNotificacion.Venta => "💰",
            TipoNotificacion.FinCoccion => "🔥",
            TipoNotificacion.StockBajo => "⚠️",
            TipoNotificacion.Sistema => "🔔",
            _ => "📱"
        };

        private string CalcularTiempoTranscurrido()
        {
            var tiempo = DateTime.UtcNow - FechaCreacion;
            
            if (tiempo.TotalMinutes < 1)
                return "Hace unos segundos";
            if (tiempo.TotalMinutes < 60)
                return $"Hace {tiempo.Minutes} minuto{(tiempo.Minutes != 1 ? "s" : "")}";
            if (tiempo.TotalHours < 24)
                return $"Hace {tiempo.Hours} hora{(tiempo.Hours != 1 ? "s" : "")}";
            if (tiempo.TotalDays < 7)
                return $"Hace {tiempo.Days} día{(tiempo.Days != 1 ? "s" : "")}";
            
            return FechaCreacion.ToString("dd/MM/yyyy HH:mm");
        }
    }

    // =============================================
    // DTOs PARA CREAR NOTIFICACIONES
    // =============================================

    /// <summary>
    /// DTO para crear notificación de venta
    /// </summary>
    public class CrearNotificacionVentaDto
    {
        [Required(ErrorMessage = "El ID de la venta es obligatorio")]
        public int VentaId { get; set; }
    }

    /// <summary>
    /// DTO para crear notificación de fin de cocción
    /// </summary>
    public class CrearNotificacionFinCoccionDto
    {
        [Required(ErrorMessage = "El ID de la sucursal es obligatorio")]
        public int SucursalId { get; set; }

        [Required(ErrorMessage = "El tipo de producto es obligatorio")]
        [StringLength(100, ErrorMessage = "El tipo de producto no puede exceder 100 caracteres")]
        public string TipoProducto { get; set; } = string.Empty;

        [Required(ErrorMessage = "La cantidad es obligatoria")]
        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0")]
        public int Cantidad { get; set; }

        public int? LoteId { get; set; }

        [StringLength(200, ErrorMessage = "Las notas no pueden exceder 200 caracteres")]
        public string? Notas { get; set; }
    }

    /// <summary>
    /// DTO para crear notificación de stock bajo
    /// </summary>
    public class CrearNotificacionStockBajoDto
    {
        [Required(ErrorMessage = "El ID del stock es obligatorio")]
        public int StockId { get; set; }
    }

    /// <summary>
    /// DTO para crear notificación personalizada del sistema
    /// </summary>
    public class CrearNotificacionSistemaDto
    {
        [Required(ErrorMessage = "El ID de la sucursal es obligatorio")]
        public int SucursalId { get; set; }

        [Required(ErrorMessage = "El título es obligatorio")]
        [StringLength(100, ErrorMessage = "El título no puede exceder 100 caracteres")]
        public string Titulo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El mensaje es obligatorio")]
        [StringLength(300, ErrorMessage = "El mensaje no puede exceder 300 caracteres")]
        public string Mensaje { get; set; } = string.Empty;

        public int? ReferenciaId { get; set; }

        public bool EnviarInmediatamente { get; set; } = true;
    }

    /// <summary>
    /// DTO para marcar múltiples notificaciones como enviadas
    /// </summary>
    public class MarcarEnviadasDto
    {
        [Required(ErrorMessage = "Debe especificar al menos una notificación")]
        [MinLength(1, ErrorMessage = "Debe especificar al menos una notificación")]
        public List<int> NotificacionIds { get; set; } = new();
    }

    // =============================================
    // DTOs DE ESTADÍSTICAS Y REPORTES
    // =============================================

    /// <summary>
    /// DTO para estadísticas generales de notificaciones
    /// </summary>
    public class EstadisticasNotificacionesDto
    {
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public int TotalNotificaciones { get; set; }
        public int NotificacionesEnviadas { get; set; }
        public int NotificacionesPendientes { get; set; }
        public double PorcentajeExito { get; set; }
        public List<NotificacionPorTipoDto> NotificacionesPorTipo { get; set; } = new();
        public List<NotificacionPorSucursalDto> NotificacionesPorSucursal { get; set; } = new();
        public double TasaExito => TotalNotificaciones > 0 ? 
            (NotificacionesEnviadas * 100.0) / TotalNotificaciones : 100;
        public string RendimientoTexto => TasaExito switch
        {
            >= 95 => "Excelente",
            >= 85 => "Bueno",
            >= 70 => "Regular",
            _ => "Deficiente"
        };
    }

    /// <summary>
    /// DTO para notificaciones agrupadas por tipo
    /// </summary>
    public class NotificacionPorTipoDto
    {
        public TipoNotificacion Tipo { get; set; }
        public string TipoTexto => Tipo.ToString();
        public int Cantidad { get; set; }
        public int Enviadas { get; set; }
        public int Pendientes { get; set; }
        public double PorcentajeExito => Cantidad > 0 ? (Enviadas * 100.0) / Cantidad : 100;
        public string IconoTipo => Tipo switch
        {
            TipoNotificacion.Venta => "💰",
            TipoNotificacion.FinCoccion => "🔥",
            TipoNotificacion.StockBajo => "⚠️",
            TipoNotificacion.Sistema => "🔔",
            _ => "📱"
        };
    }

    /// <summary>
    /// DTO para notificaciones agrupadas por sucursal
    /// </summary>
    public class NotificacionPorSucursalDto
    {
        public int SucursalId { get; set; }
        public string SucursalNombre { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public int Enviadas { get; set; }
        public int Pendientes { get; set; }
        public double PorcentajeExito => Cantidad > 0 ? (Enviadas * 100.0) / Cantidad : 100;
        public string RendimientoTexto => PorcentajeExito switch
        {
            >= 95 => "Excelente",
            >= 85 => "Bueno",
            >= 70 => "Regular",
            _ => "Deficiente"
        };
    }

    /// <summary>
    /// DTO para resultado de procesamiento masivo
    /// </summary>
    public class ResultadoProcesamientoDto
    {
        public int TotalProcesadas { get; set; }
        public int Exitosas { get; set; }
        public int Fallidas { get; set; }
        public double PorcentajeExito { get; set; }
        public DateTime FechaProcesamiento { get; set; } = DateTime.UtcNow;
        public string ResultadoTexto => PorcentajeExito switch
        {
            100 => "Procesamiento exitoso",
            >= 90 => "Procesamiento mayormente exitoso",
            >= 70 => "Procesamiento con algunas fallas",
            _ => "Procesamiento con múltiples fallas"
        };
        public bool EsExitoso => PorcentajeExito >= 90;
    }

    // =============================================
    // DTOs DE CONFIGURACIÓN
    // =============================================

    /// <summary>
    /// DTO para configuración de notificaciones por sucursal
    /// </summary>
    public class ConfiguracionNotificacionesDto
    {
        public int SucursalId { get; set; }
        public bool NotificacionesVentaHabilitadas { get; set; } = true;
        public bool NotificacionesFinCoccionHabilitadas { get; set; } = true;
        public bool NotificacionesStockBajoHabilitadas { get; set; } = true;
        public bool NotificacionesSistemaHabilitadas { get; set; } = true;
        public int IntervaloMinimoMinutos { get; set; } = 5; // Para evitar spam
        public List<string> DispositiosRegistrados { get; set; } = new();
        public string? TokenFirebase { get; set; }
        public DateTime UltimaActualizacion { get; set; }
    }

    /// <summary>
    /// DTO para registro de dispositivo móvil
    /// </summary>
    public class RegistrarDispositivoDto
    {
        [Required(ErrorMessage = "El ID de la sucursal es obligatorio")]
        public int SucursalId { get; set; }

        [Required(ErrorMessage = "El token del dispositivo es obligatorio")]
        [StringLength(500, ErrorMessage = "El token no puede exceder 500 caracteres")]
        public string TokenDispositivo { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "El tipo de dispositivo no puede exceder 50 caracteres")]
        public string TipoDispositivo { get; set; } = "mobile"; // mobile, tablet, web

        [StringLength(100, ErrorMessage = "El nombre del usuario no puede exceder 100 caracteres")]
        public string? NombreUsuario { get; set; }
    }

    /// <summary>
    /// DTO para plantilla de notificación
    /// </summary>
    public class PlantillaNotificacionDto
    {
        public int Id { get; set; }
        public TipoNotificacion TipoNotificacion { get; set; }
        public string TipoTexto => TipoNotificacion.ToString();
        public string PlantillaTitulo { get; set; } = string.Empty;
        public string PlantillaMensaje { get; set; } = string.Empty;
        public bool Activa { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime FechaActualizacion { get; set; }
        public List<string> VariablesDisponibles { get; set; } = new();
        public string EjemploTitulo { get; set; } = string.Empty;
        public string EjemploMensaje { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO para crear o actualizar plantilla
    /// </summary>
    public class CrearPlantillaNotificacionDto
    {
        [Required(ErrorMessage = "El tipo de notificación es obligatorio")]
        public TipoNotificacion TipoNotificacion { get; set; }

        [Required(ErrorMessage = "La plantilla del título es obligatoria")]
        [StringLength(100, ErrorMessage = "La plantilla del título no puede exceder 100 caracteres")]
        public string PlantillaTitulo { get; set; } = string.Empty;

        [Required(ErrorMessage = "La plantilla del mensaje es obligatoria")]
        [StringLength(300, ErrorMessage = "La plantilla del mensaje no puede exceder 300 caracteres")]
        public string PlantillaMensaje { get; set; } = string.Empty;

        public bool Activa { get; set; } = true;
    }

    // =============================================
    // DTOs DE ANÁLISIS Y MÉTRICAS
    // =============================================

    /// <summary>
    /// DTO para métricas de rendimiento de notificaciones
    /// </summary>
    public class MetricasNotificacionesDto
    {
        public DateTime FechaAnalisis { get; set; } = DateTime.UtcNow;
        public TimeSpan PeriodoAnalisis { get; set; }
        public double TiempoPromedioEnvio { get; set; } // En segundos
        public double TasaEntregaExitosa { get; set; }
        public int TotalNotificacionesPeriodo { get; set; }
        public int PicoMaximoNotificacionesHora { get; set; }
        public string HoraPico { get; set; } = string.Empty;
        public List<TendenciaPorHoraDto> TendenciaPorHora { get; set; } = new();
        public List<RendimientoPorTipoDto> RendimientoPorTipo { get; set; } = new();
        public string AnalisisTexto { get; set; } = string.Empty;
        public List<string> Recomendaciones { get; set; } = new();
    }

    /// <summary>
    /// DTO para tendencia de notificaciones por hora
    /// </summary>
    public class TendenciaPorHoraDto
    {
        public int Hora { get; set; }
        public string HoraTexto => $"{Hora:00}:00";
        public int CantidadNotificaciones { get; set; }
        public double TasaExito { get; set; }
        public double TiempoPromedioEnvio { get; set; }
    }

    /// <summary>
    /// DTO para rendimiento por tipo de notificación
    /// </summary>
    public class RendimientoPorTipoDto
    {
        public TipoNotificacion Tipo { get; set; }
        public string TipoTexto => Tipo.ToString();
        public int CantidadEnviadas { get; set; }
        public double TasaExito { get; set; }
        public double TiempoPromedioEnvio { get; set; }
        public string Rendimiento => TasaExito switch
        {
            >= 98 => "Excelente",
            >= 90 => "Bueno",
            >= 80 => "Regular",
            _ => "Deficiente"
        };
    }
}