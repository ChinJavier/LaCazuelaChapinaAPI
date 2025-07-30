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
using LaCazuelaChapina.API.Models.Sucursales;
using LaCazuelaChapina.API.DTOs.Sucursales;
using LaCazuelaChapina.API.DTOs.Notificaciones;
using LaCazuelaChapina.API.Models.Notificaciones;
using LaCazuelaChapina.API.DTOs.LLM;

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
            ConfigureSucursalesMappings();
            ConfigureNotificacionesMappings();
            ConfigureLLMMappings();
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

            // TipoAtributo -> TipoAtributoDetalleDto
            CreateMap<TipoAtributo, TipoAtributoDetalleDto>()
                .ForMember(dest => dest.CategoriaNombre, opt => opt.MapFrom(src => src.Categoria.Nombre))
                .ForMember(dest => dest.OpcionesActivas, opt => opt.Ignore()) // Se calcula en el controller
                .ForMember(dest => dest.VecesUtilizado, opt => opt.Ignore()) // Se calcula en el controller
                .ForMember(dest => dest.UltimoUso, opt => opt.Ignore()) // Se calcula en el controller
                .ForMember(dest => dest.EstadoUso, opt => opt.Ignore()); // Se calcula en el DTO

            // CrearTipoAtributoDto -> TipoAtributo
            CreateMap<CrearTipoAtributoDto, TipoAtributo>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Orden, opt => opt.Ignore()) // Se calcula en el controller
                .ForMember(dest => dest.Categoria, opt => opt.Ignore())
                .ForMember(dest => dest.Opciones, opt => opt.Ignore())
                .ForMember(dest => dest.PersonalizacionesVenta, opt => opt.Ignore());

            // ActualizarTipoAtributoDto -> TipoAtributo
            CreateMap<ActualizarTipoAtributoDto, TipoAtributo>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CategoriaId, opt => opt.Ignore())
                .ForMember(dest => dest.Orden, opt => opt.Ignore())
                .ForMember(dest => dest.Categoria, opt => opt.Ignore())
                .ForMember(dest => dest.Opciones, opt => opt.Ignore())
                .ForMember(dest => dest.PersonalizacionesVenta, opt => opt.Ignore());

            // OpcionAtributo -> OpcionAtributoDto
            CreateMap<OpcionAtributo, OpcionAtributoDto>();

            // OpcionAtributo -> OpcionAtributoDetalleDto
            CreateMap<OpcionAtributo, OpcionAtributoDetalleDto>()
                .ForMember(dest => dest.TipoAtributoNombre, opt => opt.MapFrom(src => src.TipoAtributo.Nombre))
                .ForMember(dest => dest.CategoriaNombre, opt => opt.MapFrom(src => src.TipoAtributo.Categoria.Nombre))
                .ForMember(dest => dest.VecesUtilizada, opt => opt.Ignore()) // Se calcula en el controller
                .ForMember(dest => dest.UltimoUso, opt => opt.Ignore()) // Se calcula en el controller
                .ForMember(dest => dest.EstadoUso, opt => opt.Ignore()) // Se calcula en el DTO
                .ForMember(dest => dest.IngresosGenerados, opt => opt.Ignore()); // Se calcula en el DTO

            // CrearOpcionAtributoDto -> OpcionAtributo
            CreateMap<CrearOpcionAtributoDto, OpcionAtributo>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.TipoAtributoId, opt => opt.Ignore()) // Se establece en el controller
                .ForMember(dest => dest.Activa, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.Orden, opt => opt.Ignore()) // Se calcula en el controller
                .ForMember(dest => dest.TipoAtributo, opt => opt.Ignore())
                .ForMember(dest => dest.PersonalizacionesVenta, opt => opt.Ignore());

            // ActualizarOpcionAtributoDto -> OpcionAtributo
            CreateMap<ActualizarOpcionAtributoDto, OpcionAtributo>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.TipoAtributoId, opt => opt.Ignore())
                .ForMember(dest => dest.Orden, opt => opt.Ignore())
                .ForMember(dest => dest.TipoAtributo, opt => opt.Ignore())
                .ForMember(dest => dest.PersonalizacionesVenta, opt => opt.Ignore());

            // PersonalizacionDto -> PersonalizacionVenta
            CreateMap<PersonalizacionDto, PersonalizacionVenta>();
        }

        private void ConfigureComboMappings()
        {
            // Combo -> ComboDto
            CreateMap<Combo, ComboDto>()
                .ForMember(dest => dest.Componentes, opt => opt.MapFrom(src => src.Componentes));

            // Combo -> ComboDetalleDto
            CreateMap<Combo, ComboDetalleDto>()
                .ForMember(dest => dest.Componentes, opt => opt.MapFrom(src => src.Componentes))
                .ForMember(dest => dest.VecesVendido, opt => opt.Ignore()) // Se calcula en el controller
                .ForMember(dest => dest.IngresosGenerados, opt => opt.Ignore()) // Se calcula en el controller
                .ForMember(dest => dest.EstaVigente, opt => opt.Ignore()) // Se calcula en el controller
                .ForMember(dest => dest.UltimaVenta, opt => opt.Ignore()) // Se calcula en el controller
                .ForMember(dest => dest.EstadoVigencia, opt => opt.Ignore()) // Se calcula en el DTO
                .ForMember(dest => dest.PromedioVentasMensuales, opt => opt.Ignore()) // Se calcula en el DTO
                .ForMember(dest => dest.RendimientoTexto, opt => opt.Ignore()); // Se calcula en el DTO

            // Combo -> ComboEstacionalDto
            CreateMap<Combo, ComboEstacionalDto>()
                .ForMember(dest => dest.Componentes, opt => opt.MapFrom(src => src.Componentes))
                .ForMember(dest => dest.DiasRestantes, opt => opt.Ignore()) // Se calcula en el controller
                .ForMember(dest => dest.EstadoVigencia, opt => opt.Ignore()) // Se calcula en el controller
                .ForMember(dest => dest.EsProximo, opt => opt.Ignore()) // Se calcula en el DTO
                .ForMember(dest => dest.EstaVigente, opt => opt.Ignore()) // Se calcula en el DTO
                .ForMember(dest => dest.IconoEstado, opt => opt.Ignore()) // Se calcula en el DTO
                .ForMember(dest => dest.ColorEstado, opt => opt.Ignore()); // Se calcula en el DTO

            // CrearComboDto -> Combo
            CreateMap<CrearComboDto, Combo>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Activo, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.FechaCreacion, opt => opt.Ignore()) // Se establece en el controller
                .ForMember(dest => dest.Componentes, opt => opt.Ignore()) // Se manejan por separado
                .ForMember(dest => dest.DetalleVentas, opt => opt.Ignore());

            // ActualizarComboDto -> Combo
            CreateMap<ActualizarComboDto, Combo>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.TipoCombo, opt => opt.Ignore()) // No se puede cambiar el tipo
                .ForMember(dest => dest.FechaCreacion, opt => opt.Ignore())
                .ForMember(dest => dest.Componentes, opt => opt.Ignore()) // Se manejan por separado
                .ForMember(dest => dest.DetalleVentas, opt => opt.Ignore());

            // ComboComponente -> ComboComponenteDto
            CreateMap<ComboComponente, ComboComponenteDto>()
                .ForMember(dest => dest.ProductoNombre, opt => opt.MapFrom(src => src.Producto != null ? src.Producto.Nombre : src.NombreEspecial))
                .ForMember(dest => dest.VarianteNombre, opt => opt.MapFrom(src => src.VarianteProducto != null ? src.VarianteProducto.Nombre : null));

            // ComboComponenteDto -> ComboComponente (para creación/edición)
            CreateMap<ComboComponenteDto, ComboComponente>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.ComboId, opt => opt.Ignore()) // Se establece en el controller
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

    // ⭐ MAPEO PRINCIPAL ACTUALIZADO - Venta -> VentaDto
    CreateMap<Venta, VentaDto>()
        .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
        .ForMember(dest => dest.NumeroVenta, opt => opt.MapFrom(src => src.NumeroVenta))
        .ForMember(dest => dest.FechaVenta, opt => opt.MapFrom(src => src.FechaVenta))
        .ForMember(dest => dest.Subtotal, opt => opt.MapFrom(src => src.Subtotal))
        .ForMember(dest => dest.Descuento, opt => opt.MapFrom(src => src.Descuento))
        .ForMember(dest => dest.Total, opt => opt.MapFrom(src => src.Total))
        .ForMember(dest => dest.TipoPago, opt => opt.MapFrom(src => src.TipoPago))
        .ForMember(dest => dest.EstadoVenta, opt => opt.MapFrom(src => src.EstadoVenta))
        .ForMember(dest => dest.ClienteNombre, opt => opt.MapFrom(src => src.ClienteNombre))
        .ForMember(dest => dest.ClienteTelefono, opt => opt.MapFrom(src => src.ClienteTelefono))
        .ForMember(dest => dest.EsVentaOffline, opt => opt.MapFrom(src => src.EsVentaOffline))
        .ForMember(dest => dest.SucursalNombre, opt => opt.MapFrom(src => src.Sucursal != null ? src.Sucursal.Nombre : ""))
        .ForMember(dest => dest.Detalles, opt => opt.MapFrom(src => src.Detalles));

    // Venta -> VentaResumenDto  
    CreateMap<Venta, VentaResumenDto>()
        .ForMember(dest => dest.SucursalNombre, opt => opt.MapFrom(src => src.Sucursal != null ? src.Sucursal.Nombre : ""))
        .ForMember(dest => dest.CantidadItems, opt => opt.MapFrom(src => src.Detalles.Sum(d => d.Cantidad)));

    // DetalleVenta -> DetalleVentaResponseDto
    CreateMap<DetalleVenta, DetalleVentaResponseDto>()
        .ForMember(dest => dest.ProductoNombre,
            opt => opt.MapFrom(src => src.Producto != null ? src.Producto.Nombre : 
                                     src.Combo != null ? src.Combo.Nombre : ""))
        .ForMember(dest => dest.VarianteNombre,
            opt => opt.MapFrom(src => src.VarianteProducto != null ? src.VarianteProducto.Nombre : null))
        .ForMember(dest => dest.Personalizaciones, opt => opt.MapFrom(src => src.Personalizaciones));

    // PersonalizacionVenta -> PersonalizacionResponseDto
    CreateMap<PersonalizacionVenta, PersonalizacionResponseDto>()
        .ForMember(dest => dest.TipoAtributoNombre, opt => opt.MapFrom(src => src.TipoAtributo != null ? src.TipoAtributo.Nombre : ""))
        .ForMember(dest => dest.OpcionNombre, opt => opt.MapFrom(src => src.OpcionAtributo != null ? src.OpcionAtributo.Nombre : ""));
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

        private void ConfigureSucursalesMappings()
        {
            // Sucursal -> SucursalDto
            CreateMap<Sucursal, SucursalDto>();

            // Sucursal -> SucursalDetalleDto
            CreateMap<Sucursal, SucursalDetalleDto>()
                .ForMember(dest => dest.VentasHoy, opt => opt.Ignore())
                .ForMember(dest => dest.IngresosDiarios, opt => opt.Ignore())
                .ForMember(dest => dest.ProductosCriticos, opt => opt.Ignore())
                .ForMember(dest => dest.UltimaVenta, opt => opt.Ignore())
                .ForMember(dest => dest.EstadoOperativo, opt => opt.Ignore());

            // CrearSucursalDto -> Sucursal
            CreateMap<CrearSucursalDto, Sucursal>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Activa, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.FechaCreacion, opt => opt.Ignore())
                .ForMember(dest => dest.FechaActualizacion, opt => opt.Ignore());

            // ActualizarSucursalDto -> Sucursal
            CreateMap<ActualizarSucursalDto, Sucursal>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.FechaCreacion, opt => opt.Ignore())
                .ForMember(dest => dest.FechaActualizacion, opt => opt.Ignore());
        }

        private void ConfigureNotificacionesMappings()
        {
            // Notificacion -> NotificacionDto
            CreateMap<Notificacion, NotificacionDto>()
                .ForMember(dest => dest.SucursalNombre, opt => opt.MapFrom(src => src.Sucursal.Nombre))
                .ForMember(dest => dest.TipoNotificacionTexto, opt => opt.Ignore()) // Se calcula en el DTO
                .ForMember(dest => dest.TiempoTranscurrido, opt => opt.Ignore()) // Se calcula en el DTO
                .ForMember(dest => dest.EstadoTexto, opt => opt.Ignore()) // Se calcula en el DTO
                .ForMember(dest => dest.IconoTipo, opt => opt.Ignore()); // Se calcula en el DTO

            // CrearNotificacionVentaDto -> Notificacion (mapeo básico, se completa en el controller)
            CreateMap<CrearNotificacionVentaDto, Notificacion>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.SucursalId, opt => opt.Ignore()) // Se establece desde la venta
                .ForMember(dest => dest.TipoNotificacion, opt => opt.MapFrom(src => TipoNotificacion.Venta))
                .ForMember(dest => dest.Titulo, opt => opt.Ignore()) // Se genera en el controller
                .ForMember(dest => dest.Mensaje, opt => opt.Ignore()) // Se genera en el controller
                .ForMember(dest => dest.FechaCreacion, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.Enviada, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.FechaEnvio, opt => opt.Ignore())
                .ForMember(dest => dest.ReferenciaId, opt => opt.MapFrom(src => src.VentaId))
                .ForMember(dest => dest.Sucursal, opt => opt.Ignore());

            // CrearNotificacionFinCoccionDto -> Notificacion
            CreateMap<CrearNotificacionFinCoccionDto, Notificacion>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.TipoNotificacion, opt => opt.MapFrom(src => TipoNotificacion.FinCoccion))
                .ForMember(dest => dest.Titulo, opt => opt.MapFrom(src => "Lote de Cocción Completado"))
                .ForMember(dest => dest.Mensaje, opt => opt.MapFrom(src =>
                    $"Lote de {src.TipoProducto} completado - {src.Cantidad} unidades listas"))
                .ForMember(dest => dest.FechaCreacion, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.Enviada, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.FechaEnvio, opt => opt.Ignore())
                .ForMember(dest => dest.ReferenciaId, opt => opt.MapFrom(src => src.LoteId))
                .ForMember(dest => dest.Sucursal, opt => opt.Ignore());

            // CrearNotificacionSistemaDto -> Notificacion
            CreateMap<CrearNotificacionSistemaDto, Notificacion>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.TipoNotificacion, opt => opt.MapFrom(src => TipoNotificacion.Sistema))
                .ForMember(dest => dest.FechaCreacion, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.Enviada, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.FechaEnvio, opt => opt.Ignore())
                .ForMember(dest => dest.Sucursal, opt => opt.Ignore());
        }

                private void ConfigureLLMMappings()
        {
            // Mapeos básicos para DTOs de LLM (la mayoría se construyen manualmente)
            CreateMap<AsistentePedidoRequestDto, AsistentePedidoResponseDto>()
                .ForMember(dest => dest.Recomendaciones, opt => opt.Ignore())
                .ForMember(dest => dest.TotalAproximado, opt => opt.Ignore())
                .ForMember(dest => dest.MensajePersonalizado, opt => opt.Ignore())
                .ForMember(dest => dest.ConsejosAdicionales, opt => opt.Ignore())
                .ForMember(dest => dest.FechaRecomendacion, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.ConfianzaRecomendacion, opt => opt.MapFrom(src => "Alta"));

            // La mayoría de mapeos de LLM se hacen manualmente en el controller
            // porque involucran procesamiento de IA y construcción dinámica de respuestas
        }
    }


}