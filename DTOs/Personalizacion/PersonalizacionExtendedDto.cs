// =============================================
// ARCHIVO: DTOs/Personalizacion/PersonalizacionExtendedDto.cs
// DTOs extendidos para gestión de personalización
// =============================================

using System.ComponentModel.DataAnnotations;

namespace LaCazuelaChapina.API.DTOs.Personalizacion
{
    // =============================================
    // DTOs PARA TIPOS DE ATRIBUTO
    // =============================================

    /// <summary>
    /// DTO detallado para un tipo de atributo con estadísticas
    /// </summary>
    public class TipoAtributoDetalleDto : TipoAtributoDto
    {
        public string CategoriaNombre { get; set; } = string.Empty;
        public int OpcionesActivas { get; set; }
        public int VecesUtilizado { get; set; }
        public DateTime? UltimoUso { get; set; }
        public string EstadoUso => VecesUtilizado switch
        {
            0 => "Sin uso",
            < 10 => "Poco usado",
            < 50 => "Uso moderado",
            < 100 => "Muy usado",
            _ => "Extremadamente usado"
        };
    }

    /// <summary>
    /// DTO para crear un nuevo tipo de atributo
    /// </summary>
    public class CrearTipoAtributoDto
    {
        [Required(ErrorMessage = "El nombre del tipo de atributo es obligatorio")]
        [StringLength(50, ErrorMessage = "El nombre no puede exceder 50 caracteres")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "La categoría es obligatoria")]
        public int CategoriaId { get; set; }

        public bool EsObligatorio { get; set; } = true;

        public bool PermiteMultiple { get; set; } = false;
    }

    /// <summary>
    /// DTO para actualizar un tipo de atributo
    /// </summary>
    public class ActualizarTipoAtributoDto
    {
        [Required(ErrorMessage = "El nombre del tipo de atributo es obligatorio")]
        [StringLength(50, ErrorMessage = "El nombre no puede exceder 50 caracteres")]
        public string Nombre { get; set; } = string.Empty;

        public bool EsObligatorio { get; set; }

        public bool PermiteMultiple { get; set; }
    }

    /// <summary>
    /// DTO para reordenar tipos de atributo
    /// </summary>
    public class ReordenarTiposAtributoDto
    {
        [Required(ErrorMessage = "Debe especificar el orden de los tipos de atributo")]
        [MinLength(1, ErrorMessage = "Debe especificar al menos un tipo de atributo")]
        public List<int> IdsOrdenados { get; set; } = new();
    }

    /// <summary>
    /// DTO para duplicar un tipo de atributo
    /// </summary>
    public class DuplicarTipoAtributoDto
    {
        [Required(ErrorMessage = "El nuevo nombre es obligatorio")]
        [StringLength(50, ErrorMessage = "El nombre no puede exceder 50 caracteres")]
        public string NuevoNombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "La categoría destino es obligatoria")]
        public int NuevaCategoriaId { get; set; }
    }

    // =============================================
    // DTOs PARA OPCIONES DE ATRIBUTO
    // =============================================

    /// <summary>
    /// DTO detallado para una opción de atributo con estadísticas
    /// </summary>
    public class OpcionAtributoDetalleDto : OpcionAtributoDto
    {
        public string TipoAtributoNombre { get; set; } = string.Empty;
        public string CategoriaNombre { get; set; } = string.Empty;
        public int VecesUtilizada { get; set; }
        public DateTime? UltimoUso { get; set; }
        public string EstadoUso => VecesUtilizada switch
        {
            0 => "Sin uso",
            < 5 => "Poco usada",
            < 20 => "Uso moderado",
            < 50 => "Muy usada",
            _ => "Extremadamente usada"
        };
        public decimal IngresosGenerados => VecesUtilizada * PrecioAdicional;
    }

    /// <summary>
    /// DTO para crear una nueva opción de atributo
    /// </summary>
    public class CrearOpcionAtributoDto
    {
        [Required(ErrorMessage = "El nombre de la opción es obligatorio")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        public string Nombre { get; set; } = string.Empty;

        [Range(0, double.MaxValue, ErrorMessage = "El precio adicional no puede ser negativo")]
        public decimal PrecioAdicional { get; set; } = 0;
    }

    /// <summary>
    /// DTO para actualizar una opción de atributo
    /// </summary>
    public class ActualizarOpcionAtributoDto
    {
        [Required(ErrorMessage = "El nombre de la opción es obligatorio")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        public string Nombre { get; set; } = string.Empty;

        [Range(0, double.MaxValue, ErrorMessage = "El precio adicional no puede ser negativo")]
        public decimal PrecioAdicional { get; set; }

        public bool Activa { get; set; } = true;
    }

    /// <summary>
    /// DTO para cambiar el estado de una opción
    /// </summary>
    public class CambiarEstadoOpcionDto
    {
        [Required(ErrorMessage = "Debe especificar el estado")]
        public bool Activa { get; set; }
    }

    /// <summary>
    /// DTO para reordenar opciones de atributo
    /// </summary>
    public class ReordenarOpcionesDto
    {
        [Required(ErrorMessage = "Debe especificar el orden de las opciones")]
        [MinLength(1, ErrorMessage = "Debe especificar al menos una opción")]
        public List<int> IdsOrdenados { get; set; } = new();
    }

    // =============================================
    // DTOs DE ESTADÍSTICAS
    // =============================================

    /// <summary>
    /// DTO para estadísticas generales de personalización
    /// </summary>
    public class EstadisticasPersonalizacionDto
    {
        public int TotalTiposAtributo { get; set; }
        public int TotalOpciones { get; set; }
        public int OpcionesActivas { get; set; }
        public int TotalPersonalizacionesVendidas { get; set; }
        public List<EstadisticasCategoriaDto> EstadisticasPorCategoria { get; set; } = new();
        public List<OpcionMasUsadaDto> OpcionesMasUsadas { get; set; } = new();
        public double PorcentajeOpcionesActivas => TotalOpciones > 0 ? 
            (OpcionesActivas * 100.0) / TotalOpciones : 100;
        public double PromedioPersonalizacionesPorTipo => TotalTiposAtributo > 0 ? 
            (double)TotalPersonalizacionesVendidas / TotalTiposAtributo : 0;
    }

    /// <summary>
    /// DTO para estadísticas por categoría
    /// </summary>
    public class EstadisticasCategoriaDto
    {
        public int CategoriaId { get; set; }
        public string CategoriaNombre { get; set; } = string.Empty;
        public int TiposAtributo { get; set; }
        public int TotalOpciones { get; set; }
        public int OpcionesActivas { get; set; }
        public int PersonalizacionesVendidas { get; set; }
        public double PorcentajeOpcionesActivas => TotalOpciones > 0 ? 
            (OpcionesActivas * 100.0) / TotalOpciones : 100;
        public double PromedioPersonalizacionesPorTipo => TiposAtributo > 0 ? 
            (double)PersonalizacionesVendidas / TiposAtributo : 0;
        public string NivelActividad => PersonalizacionesVendidas switch
        {
            0 => "Sin actividad",
            < 10 => "Baja actividad",
            < 50 => "Actividad moderada",
            < 100 => "Alta actividad",
            _ => "Muy alta actividad"
        };
    }

    /// <summary>
    /// DTO para opciones más utilizadas
    /// </summary>
    public class OpcionMasUsadaDto
    {
        public int OpcionId { get; set; }
        public string OpcionNombre { get; set; } = string.Empty;
        public string TipoAtributoNombre { get; set; } = string.Empty;
        public string CategoriaNombre { get; set; } = string.Empty;
        public int VecesUsada { get; set; }
        public decimal PrecioAdicional { get; set; }
        public decimal IngresosGenerados => VecesUsada * PrecioAdicional;
        public string PopularidadTexto => VecesUsada switch
        {
            < 5 => "Poco popular",
            < 20 => "Moderadamente popular",
            < 50 => "Muy popular",
            _ => "Extremadamente popular"
        };
    }

    // =============================================
    // DTOs DE CONFIGURACIÓN AVANZADA
    // =============================================

    /// <summary>
    /// DTO para configuración de precios dinámicos
    /// </summary>
    public class ConfiguracionPreciosDinamicosDto
    {
        public int OpcionId { get; set; }
        public decimal PrecioBase { get; set; }
        public bool PreciosDinamicosHabilitados { get; set; } = false;
        public decimal? PrecioHoraPico { get; set; }
        public TimeSpan? InicioHoraPico { get; set; }
        public TimeSpan? FinHoraPico { get; set; }
        public decimal? PrecioFinDeSemana { get; set; }
        public decimal? PrecioFestivo { get; set; }
        public DateTime? FechaInicioPrecioEspecial { get; set; }
        public DateTime? FechaFinPrecioEspecial { get; set; }
        public decimal? PrecioEspecial { get; set; }
    }

    /// <summary>
    /// DTO para plantillas de personalización rápida
    /// </summary>
    public class PlantillaPersonalizacionDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public int CategoriaId { get; set; }
        public string CategoriaNombre { get; set; } = string.Empty;
        public List<PersonalizacionPlantillaDto> Personalizaciones { get; set; } = new();
        public decimal PrecioAdicionalTotal { get; set; }
        public bool Activa { get; set; } = true;
        public DateTime FechaCreacion { get; set; }
        public int VecesUtilizada { get; set; }
    }

    /// <summary>
    /// DTO para personalización dentro de una plantilla
    /// </summary>
    public class PersonalizacionPlantillaDto
    {
        public int TipoAtributoId { get; set; }
        public string TipoAtributoNombre { get; set; } = string.Empty;
        public int OpcionAtributoId { get; set; }
        public string OpcionAtributoNombre { get; set; } = string.Empty;
        public decimal PrecioAdicional { get; set; }
        public bool EsObligatorio { get; set; }
    }

    /// <summary>
    /// DTO para crear plantilla de personalización
    /// </summary>
    public class CrearPlantillaPersonalizacionDto
    {
        [Required(ErrorMessage = "El nombre de la plantilla es obligatorio")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(300, ErrorMessage = "La descripción no puede exceder 300 caracteres")]
        public string? Descripcion { get; set; }

        [Required(ErrorMessage = "La categoría es obligatoria")]
        public int CategoriaId { get; set; }

        [Required(ErrorMessage = "Debe especificar al menos una personalización")]
        [MinLength(1, ErrorMessage = "Debe especificar al menos una personalización")]
        public List<CrearPersonalizacionPlantillaDto> Personalizaciones { get; set; } = new();
    }

    /// <summary>
    /// DTO para crear personalización en plantilla
    /// </summary>
    public class CrearPersonalizacionPlantillaDto
    {
        [Required(ErrorMessage = "El tipo de atributo es obligatorio")]
        public int TipoAtributoId { get; set; }

        [Required(ErrorMessage = "La opción de atributo es obligatoria")]
        public int OpcionAtributoId { get; set; }
    }

    // =============================================
    // DTOs DE ANÁLISIS Y REPORTES
    // =============================================

    /// <summary>
    /// DTO para análisis de popularidad de opciones
    /// </summary>
    public class AnalisisPopularidadDto
    {
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public int TotalVentas { get; set; }
        public int VentasConPersonalizacion { get; set; }
        public double PorcentajePersonalizacion => TotalVentas > 0 ? 
            (VentasConPersonalizacion * 100.0) / TotalVentas : 0;
        public List<PopularidadPorCategoriaDto> PopularidadPorCategoria { get; set; } = new();
        public List<TendenciaPersonalizacionDto> TendenciaSemanal { get; set; } = new();
        public List<OpcionMasRentableDto> OpcionesMasRentables { get; set; } = new();
    }

    /// <summary>
    /// DTO para popularidad por categoría
    /// </summary>
    public class PopularidadPorCategoriaDto
    {
        public int CategoriaId { get; set; }
        public string CategoriaNombre { get; set; } = string.Empty;
        public int TotalPersonalizaciones { get; set; }
        public decimal IngresosTotales { get; set; }
        public decimal PromedioPersonalizacionesPorVenta { get; set; }
        public string OpcionMasPopular { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO para tendencia de personalización
    /// </summary>
    public class TendenciaPersonalizacionDto
    {
        public DateTime Semana { get; set; }
        public int TotalPersonalizaciones { get; set; }
        public decimal IngresosPorPersonalizacion { get; set; }
        public double PromedioPersonalizacionesPorVenta { get; set; }
    }

    /// <summary>
    /// DTO para opciones más rentables
    /// </summary>
    public class OpcionMasRentableDto
    {
        public int OpcionId { get; set; }
        public string OpcionNombre { get; set; } = string.Empty;
        public string TipoAtributoNombre { get; set; } = string.Empty;
        public string CategoriaNombre { get; set; } = string.Empty;
        public int VecesVendida { get; set; }
        public decimal PrecioAdicional { get; set; }
        public decimal IngresoTotal { get; set; }
        public double MargenRentabilidad { get; set; }
        public string ClasificacionRentabilidad => MargenRentabilidad switch
        {
            >= 80 => "Muy rentable",
            >= 60 => "Rentable",
            >= 40 => "Moderadamente rentable",
            >= 20 => "Poco rentable",
            _ => "No rentable"
        };
    }
}