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
using LaCazuelaChapina.API.Models.Inventario;
using LaCazuelaChapina.API.DTOs.Inventario;
using LaCazuelaChapina.API.DTOs.Dashboard;

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
            ConfigureInventoryMappings();
            ConfigureDashboardMappings();
        }

        private void ConfigureProductMappings()
        {
            CreateMap<Producto, ProductoDto>()
                .ForMember(dest => dest.CategoriaNombre,
                    opt => opt.MapFrom(src => src.Categoria.Nombre))
                .ForMember(dest => dest.Variantes,
                    opt => opt.MapFrom(src => src.Variantes.Where(v => v.Activa)))
                .ForMember(dest => dest.AtributosPersonalizables,
                    opt => opt.MapFrom(src => src.Categoria.TiposAtributo));

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

            // Venta -> VentaDto
            CreateMap<Venta, VentaDto>()
                .ForMember(dest => dest.SucursalNombre, opt => opt.MapFrom(src => src.Sucursal.Nombre))
                .ForMember(dest => dest.Detalles, opt => opt.MapFrom(src => src.Detalles));

            // Venta -> VentaResumenDto
            CreateMap<Venta, VentaResumenDto>()
                .ForMember(dest => dest.SucursalNombre, opt => opt.MapFrom(src => src.Sucursal.Nombre))
                .ForMember(dest => dest.CantidadItems, opt => opt.MapFrom(src => src.Detalles.Sum(d => d.Cantidad)));

            // DetalleVenta -> DetalleVentaResponseDto
            CreateMap<DetalleVenta, DetalleVentaResponseDto>()
                .ForMember(dest => dest.ProductoNombre,
                    opt => opt.MapFrom(src => src.Producto != null ? src.Producto.Nombre : src.Combo!.Nombre))
                .ForMember(dest => dest.VarianteNombre,
                    opt => opt.MapFrom(src => src.VarianteProducto != null ? src.VarianteProducto.Nombre : null))
                .ForMember(dest => dest.Personalizaciones, opt => opt.MapFrom(src => src.Personalizaciones));

            // PersonalizacionVenta -> PersonalizacionResponseDto
            CreateMap<PersonalizacionVenta, PersonalizacionResponseDto>()
                .ForMember(dest => dest.TipoAtributoNombre, opt => opt.MapFrom(src => src.TipoAtributo.Nombre))
                .ForMember(dest => dest.OpcionNombre, opt => opt.MapFrom(src => src.OpcionAtributo.Nombre));
        }
        private void ConfigureDashboardMappings()
        {
            // La mayoría de los DTOs del dashboard se construyen directamente en el controller
            // porque son consultas agregadas complejas, pero agregamos algunos mapeos útiles:

            // Para mapear datos básicos cuando sea necesario
            CreateMap<Venta, VentaPorDiaDto>()
                .ForMember(dest => dest.Fecha, opt => opt.MapFrom(src => src.FechaVenta.Date))
                .ForMember(dest => dest.DiaNombre, opt => opt.MapFrom(src => 
                    src.FechaVenta.ToString("dddd", new System.Globalization.CultureInfo("es-ES"))))
                .ForMember(dest => dest.MontoVentas, opt => opt.MapFrom(src => src.Total))
                .ForMember(dest => dest.CantidadTransacciones, opt => opt.MapFrom(src => 1))
                .ForMember(dest => dest.TicketPromedio, opt => opt.MapFrom(src => src.Total))
                .ForMember(dest => dest.EsFestivo, opt => opt.MapFrom(src => false));
        }

        private void ConfigureInventoryMappings()
        {
            // StockSucursal -> StockSucursalDto
            CreateMap<StockSucursal, StockSucursalDto>()
                .ForMember(dest => dest.SucursalNombre, opt => opt.MapFrom(src => src.Sucursal.Nombre))
                .ForMember(dest => dest.MateriaPrimaNombre, opt => opt.MapFrom(src => src.MateriaPrima.Nombre))
                .ForMember(dest => dest.CategoriaNombre, opt => opt.MapFrom(src => src.MateriaPrima.Categoria.Nombre))
                .ForMember(dest => dest.UnidadMedida, opt => opt.MapFrom(src => src.MateriaPrima.UnidadMedida))
                .ForMember(dest => dest.StockMinimo, opt => opt.MapFrom(src => src.MateriaPrima.StockMinimo))
                .ForMember(dest => dest.StockMaximo, opt => opt.MapFrom(src => src.MateriaPrima.StockMaximo))
                .ForMember(dest => dest.CostoPromedio, opt => opt.MapFrom(src => src.MateriaPrima.CostoPromedio))
                .ForMember(dest => dest.PorcentajeStock, opt => opt.MapFrom(src =>
                    src.MateriaPrima.StockMinimo > 0 ? (src.CantidadActual / src.MateriaPrima.StockMinimo) * 100 : 0))
                .ForMember(dest => dest.ValorInventario, opt => opt.MapFrom(src =>
                    src.CantidadActual * src.MateriaPrima.CostoPromedio))
                .ForMember(dest => dest.RequiereReorden, opt => opt.MapFrom(src =>
                    src.CantidadActual <= src.MateriaPrima.StockMinimo))
                .ForMember(dest => dest.EstadoStock, opt => opt.Ignore()); // Se calcula en el servicio

            // MovimientoInventario -> MovimientoInventarioDto
            CreateMap<MovimientoInventario, MovimientoInventarioDto>()
                .ForMember(dest => dest.SucursalNombre, opt => opt.MapFrom(src => src.Sucursal.Nombre))
                .ForMember(dest => dest.MateriaPrimaNombre, opt => opt.MapFrom(src => src.MateriaPrima.Nombre))
                .ForMember(dest => dest.CategoriaNombre, opt => opt.MapFrom(src => src.MateriaPrima.Categoria.Nombre))
                .ForMember(dest => dest.UnidadMedida, opt => opt.MapFrom(src => src.MateriaPrima.UnidadMedida))
                .ForMember(dest => dest.StockAnterior, opt => opt.Ignore()) // Se calcula en el servicio
                .ForMember(dest => dest.StockActual, opt => opt.Ignore()); // Se calcula en el servicio

            // MateriaPrima -> MateriaPrimaDto
            CreateMap<MateriaPrima, MateriaPrimaDto>()
                .ForMember(dest => dest.CategoriaNombre, opt => opt.MapFrom(src => src.Categoria.Nombre))
                .ForMember(dest => dest.StocksPorSucursal, opt => opt.MapFrom(src => src.Stocks));

            // DTOs para crear movimientos
            CreateMap<RegistrarEntradaDto, MovimientoInventario>()
                .ForMember(dest => dest.TipoMovimiento, opt => opt.MapFrom(src => TipoMovimiento.Entrada))
                .ForMember(dest => dest.FechaMovimiento, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.MontoTotal, opt => opt.MapFrom(src => src.Cantidad * src.CostoUnitario));

            CreateMap<RegistrarSalidaDto, MovimientoInventario>()
                .ForMember(dest => dest.TipoMovimiento, opt => opt.MapFrom(src => TipoMovimiento.Salida))
                .ForMember(dest => dest.FechaMovimiento, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.CostoUnitario, opt => opt.Ignore()) // Se toma del costo promedio
                .ForMember(dest => dest.MontoTotal, opt => opt.Ignore()); // Se calcula después

            CreateMap<RegistrarMermaDto, MovimientoInventario>()
                .ForMember(dest => dest.TipoMovimiento, opt => opt.MapFrom(src => TipoMovimiento.Merma))
                .ForMember(dest => dest.FechaMovimiento, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.CostoUnitario, opt => opt.Ignore()) // Se toma del costo promedio
                .ForMember(dest => dest.MontoTotal, opt => opt.Ignore()) // Se calcula después
                .ForMember(dest => dest.Motivo, opt => opt.MapFrom(src =>
                    $"{src.TipoMerma}: {src.Motivo}"));

            CreateMap<RegistrarAjusteDto, MovimientoInventario>()
                .ForMember(dest => dest.TipoMovimiento, opt => opt.MapFrom(src => TipoMovimiento.Ajuste))
                .ForMember(dest => dest.FechaMovimiento, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.Cantidad, opt => opt.MapFrom(src =>
                    Math.Abs(src.CantidadNueva - src.CantidadAnterior)))
                .ForMember(dest => dest.CostoUnitario, opt => opt.Ignore()) // Se toma del costo promedio
                .ForMember(dest => dest.MontoTotal, opt => opt.Ignore()) // Se calcula después
                .ForMember(dest => dest.Motivo, opt => opt.MapFrom(src =>
                    $"Ajuste por {src.Motivo} - Responsable: {src.ResponsableAjuste}"));
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