// =============================================
// ARCHIVO: DTOs/Combos/CombosExtendedDto.cs
// DTOs extendidos para gestión de combos
// =============================================

using System.ComponentModel.DataAnnotations;
using LaCazuelaChapina.API.Models.Enums;

namespace LaCazuelaChapina.API.DTOs.Combos
{
    // =============================================
    // DTOs BÁSICOS EXTENDIDOS
    // =============================================

    /// <summary>
    /// DTO detallado para combo con estadísticas y estado
    /// </summary>
    public class ComboDetalleDto : ComboDto
    {
        public int VecesVendido { get; set; }
        public decimal IngresosGenerados { get; set; }
        public bool EstaVigente { get; set; }
        public DateTime? UltimaVenta { get; set; }
        public string EstadoVigencia => EstaVigente ? "Vigente" : "No vigente";
        public decimal PromedioVentasMensuales => VecesVendido > 0 ? VecesVendido / 12m : 0;
        public string RendimientoTexto => VecesVendido switch
        {
            0 => "Sin ventas",
            < 5 => "Bajo rendimiento",
            < 20 => "Rendimiento moderado",
            < 50 => "Buen rendimiento",
            _ => "Excelente rendimiento"
        };
    }

    /// <summary>
    /// DTO específico para combos estacionales con información de vigencia
    /// </summary>
    public class ComboEstacionalDto : ComboDto
    {
        public int DiasRestantes { get; set; }
        public string EstadoVigencia { get; set; } = string.Empty;
        public bool EsProximo => EstadoVigencia == "Próximo";
        public bool EstaVigente => EstadoVigencia == "Vigente";
        public string IconoEstado => EstadoVigencia switch
        {
            "Próximo" => "⏳",
            "Vigente" => "✅",
            "Expirado" => "⏰",
            _ => "❓"
        };
        public string ColorEstado => EstadoVigencia switch
        {
            "Próximo" => "warning",
            "Vigente" => "success",
            "Expirado" => "danger",
            _ => "secondary"
        };
    }

    // =============================================
    // DTOs PARA CREAR Y ACTUALIZAR COMBOS
    // =============================================

    /// <summary>
    /// DTO para crear un nuevo combo
    /// </summary>
    public class CrearComboDto
    {
        [Required(ErrorMessage = "El nombre del combo es obligatorio")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres")]
        public string? Descripcion { get; set; }

        [Required(ErrorMessage = "El precio es obligatorio")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0")]
        public decimal Precio { get; set; }

        [Required(ErrorMessage = "El tipo de combo es obligatorio")]
        public TipoCombo TipoCombo { get; set; }

        public DateTime? FechaInicioVigencia { get; set; }
        public DateTime? FechaFinVigencia { get; set; }

        public bool EsEditable { get; set; } = true;

        public List<ComboComponenteDto>? Componentes { get; set; }
    }

    /// <summary>
    /// DTO para actualizar un combo existente
    /// </summary>
    public class ActualizarComboDto
    {
        [Required(ErrorMessage = "El nombre del combo es obligatorio")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres")]
        public string? Descripcion { get; set; }

        [Required(ErrorMessage = "El precio es obligatorio")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0")]
        public decimal Precio { get; set; }

        public DateTime? FechaInicioVigencia { get; set; }
        public DateTime? FechaFinVigencia { get; set; }

        public bool Activo { get; set; } = true;
        public bool EsEditable { get; set; } = true;

        public List<ComboComponenteDto>? Componentes { get; set; }
    }

    /// <summary>
    /// DTO para cambiar el estado de un combo
    /// </summary>
    public class CambiarEstadoComboDto
    {
        [Required(ErrorMessage = "Debe especificar el estado")]
        public bool Activo { get; set; }
    }

    /// <summary>
    /// DTO para duplicar un combo
    /// </summary>
    public class DuplicarComboDto
    {
        [Required(ErrorMessage = "El nuevo nombre es obligatorio")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        public string NuevoNombre { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres")]
        public string? NuevaDescripcion { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0")]
        public decimal? NuevoPrecio { get; set; }

        [Required(ErrorMessage = "El tipo de combo es obligatorio")]
        public TipoCombo NuevoTipoCombo { get; set; }

        public DateTime? FechaInicioVigencia { get; set; }
        public DateTime? FechaFinVigencia { get; set; }
    }

    // =============================================
    // DTOs PARA PLANTILLAS ESTACIONALES
    // =============================================

    /// <summary>
    /// DTO para crear combo estacional basado en plantilla
    /// </summary>
    public class CrearComboEstacionalPlantillaDto
    {
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        public string? Nombre { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0")]
        public decimal? Precio { get; set; }

        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }

        [StringLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres")]
        public string? DescripcionPersonalizada { get; set; }
    }

    /// <summary>
    /// DTO para plantilla de combo estacional
    /// </summary>
    public class PlantillaComboEstacionalDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string Epoca { get; set; } = string.Empty;
        public int MesInicio { get; set; }
        public int MesFin { get; set; }
        public decimal PrecioSugerido { get; set; }
        public List<ComponentePlantillaDto> ComponentesSugeridos { get; set; } = new();
        public string IconoEpoca { get; set; } = string.Empty;
        public string ColorTema { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO para componente de plantilla
    /// </summary>
    public class ComponentePlantillaDto
    {
        public string Nombre { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal PrecioSugerido { get; set; }
        public string Categoria { get; set; } = string.Empty;
        public bool EsEspecial { get; set; }
    }

    // =============================================
    // DTOs DE ESTADÍSTICAS Y ANÁLISIS
    // =============================================

    /// <summary>
    /// DTO para estadísticas generales de combos
    /// </summary>
    public class EstadisticasCombosDto
    {
        public int TotalCombos { get; set; }
        public int CombosActivos { get; set; }
        public int CombosFijos { get; set; }
        public int CombosEstacionales { get; set; }
        public int CombosVigentes { get; set; }
        public int TotalVentasCombos { get; set; }
        public decimal IngresosTotalesCombos { get; set; }
        public string ComboMasVendido { get; set; } = string.Empty;
        public double PorcentajeCombosActivos => TotalCombos > 0 ? 
            (CombosActivos * 100.0) / TotalCombos : 100;
        public decimal PromedioIngresosPorCombo => CombosActivos > 0 ? 
            IngresosTotalesCombos / CombosActivos : 0;
        public double TasaExitoCombos => TotalCombos > 0 ? 
            (TotalVentasCombos * 100.0) / TotalCombos : 0;
    }

    /// <summary>
    /// DTO para análisis de rendimiento de combos
    /// </summary>
    public class AnalisisRendimientoCombosDto
    {
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public List<RendimientoComboDto> CombosOrdenadosPorVentas { get; set; } = new();
        public List<RendimientoComboDto> CombosOrdenadosPorIngresos { get; set; } = new();
        public List<TendenciaVentaComboDto> TendenciaMensual { get; set; } = new();
        public decimal IngresosTotalesPeriodo { get; set; }
        public int VentasTotalesPeriodo { get; set; }
        public string ComboMasRentable { get; set; } = string.Empty;
        public string ComboMasVendido { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO para rendimiento individual de combo
    /// </summary>
    public class RendimientoComboDto
    {
        public int ComboId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public TipoCombo TipoCombo { get; set; }
        public string TipoComboTexto => TipoCombo.ToString();
        public int VentasRealizadas { get; set; }
        public decimal IngresosGenerados { get; set; }
        public decimal MargenRentabilidad { get; set; }
        public double TasaConversion { get; set; }
        public DateTime UltimaVenta { get; set; }
        public string CategorizacionRendimiento => VentasRealizadas switch
        {
            0 => "Sin ventas",
            < 5 => "Bajo rendimiento",
            < 15 => "Rendimiento regular",
            < 30 => "Buen rendimiento",
            _ => "Excelente rendimiento"
        };
    }

    /// <summary>
    /// DTO para tendencia de ventas de combos
    /// </summary>
    public class TendenciaVentaComboDto
    {
        public DateTime Mes { get; set; }
        public string MesTexto => Mes.ToString("MMMM yyyy", new System.Globalization.CultureInfo("es-ES"));
        public int TotalVentasCombos { get; set; }
        public decimal IngresosCombos { get; set; }
        public double PromedioVentasPorDia { get; set; }
        public int CombosActivosEnMes { get; set; }
        public string EstacionalidadDetectada { get; set; } = string.Empty;
    }

    // =============================================
    // DTOs DE CONFIGURACIÓN AVANZADA
    // =============================================

    /// <summary>
    /// DTO para configuración de combo estacional automático
    /// </summary>
    public class ConfiguracionComboEstacionalDto
    {
        public int Id { get; set; }
        public string NombrePlantilla { get; set; } = string.Empty;
        public bool AutoCreacionHabilitada { get; set; }
        public int DiasAntesCreacion { get; set; } = 7;
        public int DiasAntesActivacion { get; set; } = 3;
        public bool NotificacionCreacion { get; set; } = true;
        public bool AjustePreciosAutomatico { get; set; } = false;
        public decimal FactorAjustePrecio { get; set; } = 1.0m;
        public List<int> SucursalesAplicables { get; set; } = new();
        public string ConfiguracionJson { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO para programación de combos estacionales
    /// </summary>
    public class ProgramacionComboEstacionalDto
    {
        public int Id { get; set; }
        public string NombreCombo { get; set; } = string.Empty;
        public DateTime FechaProgramada { get; set; }
        public DateTime FechaFinProgramada { get; set; }
        public bool Ejecutado { get; set; }
        public DateTime? FechaEjecucion { get; set; }
        public string EstadoProgramacion { get; set; } = string.Empty;
        public int ComboCreado { get; set; }
        public string NotasEjecucion { get; set; } = string.Empty;
        public int DiasRestantesProgramacion { get; set; }
    }

    /// <summary>
    /// DTO para validación de combo
    /// </summary>
    public class ValidacionComboDto
    {
        public bool EsValido { get; set; }
        public List<string> Errores { get; set; } = new();
        public List<string> Advertencias { get; set; } = new();
        public List<string> Sugerencias { get; set; } = new();
        public decimal PrecioCalculado { get; set; }
        public decimal PrecioSugerido { get; set; }
        public bool ComponentesDisponibles { get; set; }
        public List<ComponenteNoDisponibleDto> ComponentesFaltantes { get; set; } = new();
    }

    /// <summary>
    /// DTO para componente no disponible
    /// </summary>
    public class ComponenteNoDisponibleDto
    {
        public string NombreComponente { get; set; } = string.Empty;
        public string Motivo { get; set; } = string.Empty;
        public List<string> AlternativasSugeridas { get; set; } = new();
        public bool EsCritico { get; set; }
    }

    // =============================================
    // DTOs PARA PROMOCIONES Y DESCUENTOS
    // =============================================

    /// <summary>
    /// DTO para promoción de combo
    /// </summary>
    public class PromocionComboDto
    {
        public int Id { get; set; }
        public int ComboId { get; set; }
        public string NombrePromocion { get; set; } = string.Empty;
        public string DescripcionPromocion { get; set; } = string.Empty;
        public decimal PorcentajeDescuento { get; set; }
        public decimal MontoDescuento { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public bool Activa { get; set; }
        public int VecesUtilizada { get; set; }
        public int LimiteUso { get; set; }
        public List<int> SucursalesAplicables { get; set; } = new();
        public string CodigoPromocion { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO para crear promoción de combo
    /// </summary>
    public class CrearPromocionComboDto
    {
        [Required(ErrorMessage = "El combo es obligatorio")]
        public int ComboId { get; set; }

        [Required(ErrorMessage = "El nombre de la promoción es obligatorio")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        public string NombrePromocion { get; set; } = string.Empty;

        [StringLength(300, ErrorMessage = "La descripción no puede exceder 300 caracteres")]
        public string DescripcionPromocion { get; set; } = string.Empty;

        [Range(0, 100, ErrorMessage = "El porcentaje de descuento debe estar entre 0 y 100")]
        public decimal PorcentajeDescuento { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "El monto de descuento no puede ser negativo")]
        public decimal MontoDescuento { get; set; }

        [Required(ErrorMessage = "La fecha de inicio es obligatoria")]
        public DateTime FechaInicio { get; set; }

        [Required(ErrorMessage = "La fecha de fin es obligatoria")]
        public DateTime FechaFin { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "El límite de uso debe ser mayor a 0")]
        public int LimiteUso { get; set; } = int.MaxValue;

        public List<int> SucursalesAplicables { get; set; } = new();

        [StringLength(20, ErrorMessage = "El código de promoción no puede exceder 20 caracteres")]
        public string? CodigoPromocion { get; set; }
    }
}