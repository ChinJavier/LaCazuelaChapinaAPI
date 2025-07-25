// =============================================
// ARCHIVO: Mappings/AutoMapperProfile.cs
// Configuración de AutoMapper
// =============================================

using AutoMapper;
using LaCazuelaChapina.API.Models.Productos;
using LaCazuelaChapina.API.Models.Personalizacion;
using LaCazuelaChapina.API.Models.Combos;
using LaCazuelaChapina.API.Models.Ventas;
using LaCazuelaChapina.API.Models.Enums;
using LaCazuelaChapina.API.DTOs.Productos;
using LaCazuelaChapina.API.DTOs.Personalizacion;
using LaCazuelaChapina.API.DTOs.Ventas;
using LaCazuelaChapina.API.DTOs.Combos;

namespace LaCazuelaChapina.API.Mappings
{
    /// <summary>
    /// Perfil de AutoMapper para todas las entidades del sistema
    /// </summary>
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            ConfigureProductMappings();
            ConfigurePersonalizationMappings();
            ConfigureComboMappings();
            ConfigureSalesMappings();
            ConfigureDashboardMappings();
        }

        private void ConfigureProductMappings()
        {
            // Producto -> ProductoDto
            CreateMap<Producto, ProductoDto>()
                .ForMember(dest => dest.CategoriaNombre, opt => opt.MapFrom(src => src.Categoria.Nombre))
                .ForMember(dest => dest.Variantes, opt => opt.MapFrom(src => src.Variantes))
                .ForMember(dest => dest.AtributosPersonalizables, opt => opt.MapFrom(src => src.Categoria.TiposAtributo));

            // VarianteProducto -> VarianteProductoDto
            CreateMap<VarianteProducto, VarianteProductoDto>();
        }

        private void ConfigurePersonalizationMappings()
        {
            // TipoAtributo -> TipoAtributoDto
            CreateMap<TipoAtributo, TipoAtributoDto>()
                .ForMember(dest => dest.Opciones, opt => opt.MapFrom(src => src.Opciones.Where(o => o.Activa).OrderBy(o => o.Orden)));

            // OpcionAtributo -> OpcionAtributoDto
            CreateMap<OpcionAtributo, OpcionAtributoDto>();

            // PersonalizacionDto -> PersonalizacionVenta
            CreateMap<PersonalizacionDto, PersonalizacionVenta>();
        }

        private void ConfigureComboMappings()
        {
            // Combo -> ComboDto
            CreateMap<Combo, ComboDto>()
                .ForMember(dest => dest.Componentes, opt => opt.MapFrom(src => src.Componentes));

            // ComboComponente -> ComboComponenteDto
            CreateMap<ComboComponente, ComboComponenteDto>()
                .ForMember(dest => dest.ProductoNombre, opt => opt.MapFrom(src => src.Producto != null ? src.Producto.Nombre : src.NombreEspecial))
                .ForMember(dest => dest.VarianteNombre, opt => opt.MapFrom(src => src.VarianteProducto != null ? src.VarianteProducto.Nombre : null));

            // ComboComponenteDto -> ComboComponente (para creación/edición)
            CreateMap<ComboComponenteDto, ComboComponente>()
                .ForMember(dest => dest.Producto, opt => opt.Ignore())
                .ForMember(dest => dest.VarianteProducto, opt => opt.Ignore())
                .ForMember(dest => dest.Combo, opt => opt.Ignore());
        }

        private void ConfigureSalesMappings()
        {
            // CrearVentaDto -> Venta
            CreateMap<CrearVentaDto, Venta>()
                .ForMember(dest => dest.FechaVenta, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.EstadoVenta, opt => opt.MapFrom(src => EstadoVenta.Completada))
                .ForMember(dest => dest.Detalles, opt => opt.MapFrom(src => src.Detalles));

            // DetalleVentaDto -> DetalleVenta
            CreateMap<DetalleVentaDto, DetalleVenta>()
                .ForMember(dest => dest.Personalizaciones, opt => opt.MapFrom(src => src.Personalizaciones));

            // Venta -> VentaDto (para respuestas)
            CreateMap<Venta, VentaDto>()
                .ForMember(dest => dest.SucursalNombre, opt => opt.MapFrom(src => src.Sucursal.Nombre))
                .ForMember(dest => dest.Detalles, opt => opt.MapFrom(src => src.Detalles));

            // DetalleVenta -> DetalleVentaResponseDto
            CreateMap<DetalleVenta, DetalleVentaResponseDto>()
                .ForMember(dest => dest.ProductoNombre, opt => opt.MapFrom(src => src.Producto != null ? src.Producto.Nombre : src.Combo!.Nombre))
                .ForMember(dest => dest.VarianteNombre, opt => opt.MapFrom(src => src.VarianteProducto != null ? src.VarianteProducto.Nombre : null))
                .ForMember(dest => dest.Personalizaciones, opt => opt.MapFrom(src => src.Personalizaciones));

            // PersonalizacionVenta -> PersonalizacionResponseDto
            CreateMap<PersonalizacionVenta, PersonalizacionResponseDto>()
                .ForMember(dest => dest.TipoAtributoNombre, opt => opt.MapFrom(src => src.TipoAtributo.Nombre))
                .ForMember(dest => dest.OpcionNombre, opt => opt.MapFrom(src => src.OpcionAtributo.Nombre));
        }

        private void ConfigureDashboardMappings()
        {
            // Mapeos específicos para dashboard se crearán según las consultas necesarias
            // Estos se implementarán cuando tengamos las consultas de dashboard listas
        }
    }

    // =============================================
    // DTOs ADICIONALES PARA RESPUESTAS
    // =============================================

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
}