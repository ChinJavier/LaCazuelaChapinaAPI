using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaCazuelaChapina.API.Data;
using LaCazuelaChapina.API.Models.Enums;
using LaCazuelaChapina.API.DTOs.Dashboard;

namespace LaCazuelaChapina.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class DashboardController : ControllerBase
    {
        private readonly CazuelaDbContext _context;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(CazuelaDbContext context, ILogger<DashboardController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene el dashboard completo para una sucursal
        /// </summary>
        [HttpGet("sucursal/{sucursalId}")]
        [ProducesResponseType(typeof(DashboardDto), 200)]
        public async Task<ActionResult<DashboardDto>> GetDashboard(
            int sucursalId,
            [FromQuery] DateTime? fechaDesde = null,
            [FromQuery] DateTime? fechaHasta = null)
        {
            try
            {
                var fechaHoy = DateTime.UtcNow.Date;
                var fechaInicioMes = fechaDesde ?? new DateTime(fechaHoy.Year, fechaHoy.Month, 1);
                var fechaFinPeriodo = fechaHasta ?? fechaHoy;

                // Validar que la sucursal existe
                var sucursal = await _context.Sucursales
                    .FirstOrDefaultAsync(s => s.Id == sucursalId && s.Activa);

                if (sucursal == null)
                    return NotFound(new { message = "Sucursal no encontrada" });

                var dashboard = new DashboardDto
                {
                    VentasDiarias = await GetVentasDiarias(sucursalId, fechaHoy),
                    VentasMensuales = await GetVentasMensuales(sucursalId, fechaInicioMes, fechaFinPeriodo),
                    TransaccionesDiarias = await GetTransaccionesDiarias(sucursalId, fechaHoy),
                    TransaccionesMensuales = await GetTransaccionesMensuales(sucursalId, fechaInicioMes, fechaFinPeriodo),
                    TicketPromedio = await GetTicketPromedio(sucursalId, fechaInicioMes, fechaFinPeriodo),
                    TamalesMasVendidos = await GetTamalesMasVendidos(sucursalId, fechaInicioMes, fechaFinPeriodo),
                    BebidasPorHorario = await GetBebidasPorHorario(sucursalId, fechaHoy),
                    ProporcionPicante = await GetProporcionPicante(sucursalId, fechaInicioMes, fechaFinPeriodo),
                    UtilidadesPorLinea = await GetUtilidadesPorLinea(sucursalId, fechaInicioMes, fechaFinPeriodo),
                    DesperdicioMateriasPrimas = await GetDesperdicioMateriasPrimas(sucursalId, fechaInicioMes, fechaFinPeriodo),
                    MetricasInventario = await GetMetricasInventario(sucursalId),
                    VentasUltimos7Dias = await GetVentasUltimos7Dias(sucursalId),
                    FechaGeneracion = DateTime.UtcNow
                };

                _logger.LogInformation("📊 Dashboard generado para sucursal {SucursalId}", sucursalId);
                return Ok(dashboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error generando dashboard para sucursal {SucursalId}", sucursalId);
                return StatusCode(500, new { 
                    message = "Error interno del servidor",
                    error = ex.Message 
                });
            }
        }

        /// <summary>
        /// Obtiene métricas de ventas diarias
        /// </summary>
        [HttpGet("ventas-diarias/{sucursalId}")]
        [ProducesResponseType(typeof(decimal), 200)]
        public async Task<ActionResult<decimal>> GetVentasDiariasEndpoint(int sucursalId)
        {
            try
            {
                var fechaHoy = DateTime.UtcNow.Date;
                var ventasDiarias = await GetVentasDiarias(sucursalId, fechaHoy);
                return Ok(ventasDiarias);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo ventas diarias");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtiene los tamales más vendidos
        /// </summary>
        [HttpGet("tamales-mas-vendidos/{sucursalId}")]
        [ProducesResponseType(typeof(List<ProductoVendidoDto>), 200)]
        public async Task<ActionResult<List<ProductoVendidoDto>>> GetTamalesMasVendidosEndpoint(
            int sucursalId,
            [FromQuery] int dias = 30)
        {
            try
            {
                var fechaDesde = DateTime.UtcNow.Date.AddDays(-dias);
                var tamales = await GetTamalesMasVendidos(sucursalId, fechaDesde, DateTime.UtcNow.Date);
                return Ok(tamales);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo tamales más vendidos");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtiene análisis de bebidas por horario
        /// </summary>
        [HttpGet("bebidas-por-horario/{sucursalId}")]
        [ProducesResponseType(typeof(List<BebidaPorHorarioDto>), 200)]
        public async Task<ActionResult<List<BebidaPorHorarioDto>>> GetBebidasPorHorarioEndpoint(
            int sucursalId,
            [FromQuery] DateTime? fecha = null)
        {
            try
            {
                var fechaAnalisis = fecha ?? DateTime.UtcNow.Date;
                var bebidas = await GetBebidasPorHorario(sucursalId, fechaAnalisis);
                return Ok(bebidas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo bebidas por horario");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtiene proporción de tamales con picante vs sin picante
        /// </summary>
        [HttpGet("proporcion-picante/{sucursalId}")]
        [ProducesResponseType(typeof(ProporcionPicanteDto), 200)]
        public async Task<ActionResult<ProporcionPicanteDto>> GetProporcionPicanteEndpoint(
            int sucursalId,
            [FromQuery] int dias = 30)
        {
            try
            {
                var fechaDesde = DateTime.UtcNow.Date.AddDays(-dias);
                var proporcion = await GetProporcionPicante(sucursalId, fechaDesde, DateTime.UtcNow.Date);
                return Ok(proporcion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo proporción de picante");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtiene métricas de inventario para el dashboard
        /// </summary>
        [HttpGet("metricas-inventario/{sucursalId}")]
        [ProducesResponseType(typeof(MetricasInventarioDto), 200)]
        public async Task<ActionResult<MetricasInventarioDto>> GetMetricasInventarioEndpoint(int sucursalId)
        {
            try
            {
                var metricas = await GetMetricasInventario(sucursalId);
                return Ok(metricas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo métricas de inventario");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtiene tendencia de ventas de los últimos días
        /// </summary>
        [HttpGet("tendencia-ventas/{sucursalId}")]
        [ProducesResponseType(typeof(List<VentaPorDiaDto>), 200)]
        public async Task<ActionResult<List<VentaPorDiaDto>>> GetTendenciaVentas(
            int sucursalId,
            [FromQuery] int dias = 7)
        {
            try
            {
                var ventas = dias == 7 ? 
                    await GetVentasUltimos7Dias(sucursalId) :
                    await GetVentasUltimosDias(sucursalId, dias);
                return Ok(ventas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo tendencia de ventas");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        // =============================================
        // MÉTODOS PRIVADOS PARA CÁLCULOS
        // =============================================

        private async Task<decimal> GetVentasDiarias(int sucursalId, DateTime fecha)
        {
            var fechaUtc = DateTime.SpecifyKind(fecha, DateTimeKind.Utc);
            
            return await _context.Ventas
                .Where(v => v.SucursalId == sucursalId && 
                           v.FechaVenta.Date == fechaUtc.Date &&
                           v.EstadoVenta == EstadoVenta.Completada)
                .SumAsync(v => v.Total);
        }

        private async Task<decimal> GetVentasMensuales(int sucursalId, DateTime fechaDesde, DateTime fechaHasta)
        {
            var fechaDesdeUtc = DateTime.SpecifyKind(fechaDesde, DateTimeKind.Utc);
            var fechaHastaUtc = DateTime.SpecifyKind(fechaHasta, DateTimeKind.Utc);
            
            return await _context.Ventas
                .Where(v => v.SucursalId == sucursalId && 
                           v.FechaVenta.Date >= fechaDesdeUtc.Date &&
                           v.FechaVenta.Date <= fechaHastaUtc.Date &&
                           v.EstadoVenta == EstadoVenta.Completada)
                .SumAsync(v => v.Total);
        }

        private async Task<int> GetTransaccionesDiarias(int sucursalId, DateTime fecha)
        {
            var fechaUtc = DateTime.SpecifyKind(fecha, DateTimeKind.Utc);
            
            return await _context.Ventas
                .Where(v => v.SucursalId == sucursalId && 
                           v.FechaVenta.Date == fechaUtc.Date &&
                           v.EstadoVenta == EstadoVenta.Completada)
                .CountAsync();
        }

        private async Task<int> GetTransaccionesMensuales(int sucursalId, DateTime fechaDesde, DateTime fechaHasta)
        {
            var fechaDesdeUtc = DateTime.SpecifyKind(fechaDesde, DateTimeKind.Utc);
            var fechaHastaUtc = DateTime.SpecifyKind(fechaHasta, DateTimeKind.Utc);
            
            return await _context.Ventas
                .Where(v => v.SucursalId == sucursalId && 
                           v.FechaVenta.Date >= fechaDesdeUtc.Date &&
                           v.FechaVenta.Date <= fechaHastaUtc.Date &&
                           v.EstadoVenta == EstadoVenta.Completada)
                .CountAsync();
        }

        private async Task<decimal> GetTicketPromedio(int sucursalId, DateTime fechaDesde, DateTime fechaHasta)
        {
            var fechaDesdeUtc = DateTime.SpecifyKind(fechaDesde, DateTimeKind.Utc);
            var fechaHastaUtc = DateTime.SpecifyKind(fechaHasta, DateTimeKind.Utc);
            
            var ventas = await _context.Ventas
                .Where(v => v.SucursalId == sucursalId && 
                           v.FechaVenta.Date >= fechaDesdeUtc.Date &&
                           v.FechaVenta.Date <= fechaHastaUtc.Date &&
                           v.EstadoVenta == EstadoVenta.Completada)
                .Select(v => v.Total)
                .ToListAsync();

            return ventas.Any() ? ventas.Average() : 0;
        }

        private async Task<List<ProductoVendidoDto>> GetTamalesMasVendidos(int sucursalId, DateTime fechaDesde, DateTime fechaHasta)
        {
            var fechaDesdeUtc = DateTime.SpecifyKind(fechaDesde, DateTimeKind.Utc);
            var fechaHastaUtc = DateTime.SpecifyKind(fechaHasta, DateTimeKind.Utc);
            
            // Cargar datos en memoria primero para evitar problemas con ENUMs
            var detallesVenta = await _context.DetalleVentas
                .Include(dv => dv.Venta)
                .Include(dv => dv.Producto)
                    .ThenInclude(p => p!.Categoria)
                .Include(dv => dv.VarianteProducto)
                .Where(dv => dv.Venta.SucursalId == sucursalId &&
                           dv.Venta.FechaVenta.Date >= fechaDesdeUtc.Date &&
                           dv.Venta.FechaVenta.Date <= fechaHastaUtc.Date &&
                           dv.Producto != null)
                .ToListAsync();

            // Filtrar tamales completados en memoria
            var tamalesCompletados = detallesVenta
                .Where(dv => dv.Venta.EstadoVenta == EstadoVenta.Completada &&
                           dv.Producto!.Categoria.Nombre == "Tamales")
                .ToList();

            var tamalesVendidos = tamalesCompletados
                .GroupBy(dv => new { 
                    ProductoId = dv.ProductoId,
                    ProductoNombre = dv.Producto!.Nombre,
                    VarianteId = dv.VarianteProductoId,
                    VarianteNombre = dv.VarianteProducto!.Nombre
                })
                .Select(g => new ProductoVendidoDto
                {
                    Nombre = $"{g.Key.ProductoNombre} ({g.Key.VarianteNombre})",
                    Categoria = "Tamales",
                    CantidadVendida = g.Sum(dv => dv.Cantidad),
                    MontoTotal = g.Sum(dv => dv.Subtotal),
                    VarianteMasVendida = g.Key.VarianteNombre
                })
                .OrderByDescending(p => p.CantidadVendida)
                .Take(10)
                .ToList();

            // Calcular porcentajes
            var totalVendido = tamalesVendidos.Sum(t => t.CantidadVendida);
            foreach (var tamal in tamalesVendidos)
            {
                tamal.PorcentajeDelTotal = totalVendido > 0 ? (decimal)tamal.CantidadVendida / totalVendido * 100 : 0;
            }

            return tamalesVendidos;
        }

        private async Task<List<BebidaPorHorarioDto>> GetBebidasPorHorario(int sucursalId, DateTime fecha)
        {
            var fechaUtc = DateTime.SpecifyKind(fecha, DateTimeKind.Utc);
            
            // Consulta más robusta que maneja casos donde no hay personalizaciones
            var bebidasPorHora = await _context.DetalleVentas
                .Include(dv => dv.Venta)
                .Include(dv => dv.Producto)
                    .ThenInclude(p => p!.Categoria)
                .Include(dv => dv.Personalizaciones)
                    .ThenInclude(pv => pv.TipoAtributo)
                .Include(dv => dv.Personalizaciones)
                    .ThenInclude(pv => pv.OpcionAtributo)
                .Where(dv => dv.Venta.SucursalId == sucursalId &&
                           dv.Venta.FechaVenta.Date == fechaUtc.Date &&
                           dv.Producto != null)
                .ToListAsync();

            // Filtrar en memoria para evitar problemas con ENUMs
            var bebidasCompletadas = bebidasPorHora
                .Where(dv => dv.Venta.EstadoVenta == EstadoVenta.Completada &&
                           dv.Producto!.Categoria.Nombre == "Bebidas")
                .ToList();

            var agrupadas = bebidasCompletadas
                .GroupBy(dv => new {
                    Hora = dv.Venta.FechaVenta.Hour,
                    TipoBebida = dv.Personalizaciones
                        .FirstOrDefault(p => p.TipoAtributo.Nombre == "Tipo Bebida")?.OpcionAtributo.Nombre 
                        ?? "Bebida Estándar"
                })
                .Select(g => new BebidaPorHorarioDto
                {
                    Hora = g.Key.Hora,
                    TipoBebida = g.Key.TipoBebida,
                    CantidadVendida = g.Sum(dv => dv.Cantidad),
                    MontoVendido = g.Sum(dv => dv.Subtotal),
                    PeriodoNombre = ObtenerPeriodoDelDia(g.Key.Hora)
                })
                .OrderBy(b => b.Hora)
                .ToList();

            return agrupadas;
        }

        private async Task<ProporcionPicanteDto> GetProporcionPicante(int sucursalId, DateTime fechaDesde, DateTime fechaHasta)
        {
            var fechaDesdeUtc = DateTime.SpecifyKind(fechaDesde, DateTimeKind.Utc);
            var fechaHastaUtc = DateTime.SpecifyKind(fechaHasta, DateTimeKind.Utc);
            
            var tamalesConPersonalizacion = await _context.DetalleVentas
                .Include(dv => dv.Venta)
                .Include(dv => dv.Producto)
                    .ThenInclude(p => p!.Categoria)
                .Include(dv => dv.Personalizaciones)
                    .ThenInclude(pv => pv.TipoAtributo)
                .Include(dv => dv.Personalizaciones)
                    .ThenInclude(pv => pv.OpcionAtributo)
                .Where(dv => dv.Venta.SucursalId == sucursalId &&
                           dv.Venta.FechaVenta.Date >= fechaDesdeUtc.Date &&
                           dv.Venta.FechaVenta.Date <= fechaHastaUtc.Date &&
                           dv.Producto != null)
                .ToListAsync();

            // Filtrar en memoria
            var tamalesCompletados = tamalesConPersonalizacion
                .Where(dv => dv.Venta.EstadoVenta == EstadoVenta.Completada &&
                           dv.Producto!.Categoria.Nombre == "Tamales")
                .ToList();

            var detallesPorNivel = tamalesCompletados
                .SelectMany(dv => dv.Personalizaciones
                    .Where(p => p.TipoAtributo.Nombre == "Picante")
                    .Select(p => new { 
                        Nivel = p.OpcionAtributo.Nombre,
                        Cantidad = dv.Cantidad 
                    }))
                .GroupBy(x => x.Nivel)
                .Select(g => new DetallePicanteDto
                {
                    NivelPicante = g.Key,
                    Cantidad = g.Sum(x => x.Cantidad)
                })
                .ToList();

            var totalTamales = detallesPorNivel.Sum(d => d.Cantidad);
            
            // Calcular porcentajes
            foreach (var detalle in detallesPorNivel)
            {
                detalle.Porcentaje = totalTamales > 0 ? (decimal)detalle.Cantidad / totalTamales * 100 : 0;
            }

            var totalConPicante = detallesPorNivel
                .Where(d => d.NivelPicante != "Sin Chile")
                .Sum(d => d.Cantidad);
            
            var totalSinPicante = detallesPorNivel
                .Where(d => d.NivelPicante == "Sin Chile")
                .Sum(d => d.Cantidad);

            return new ProporcionPicanteDto
            {
                TotalConPicante = totalConPicante,
                TotalSinPicante = totalSinPicante,
                PorcentajeConPicante = totalTamales > 0 ? (decimal)totalConPicante / totalTamales * 100 : 0,
                DetallePorNivel = detallesPorNivel.OrderByDescending(d => d.Cantidad).ToList()
            };
        }

        private async Task<List<UtilidadLineaDto>> GetUtilidadesPorLinea(int sucursalId, DateTime fechaDesde, DateTime fechaHasta)
        {
            var fechaDesdeUtc = DateTime.SpecifyKind(fechaDesde, DateTimeKind.Utc);
            var fechaHastaUtc = DateTime.SpecifyKind(fechaHasta, DateTimeKind.Utc);
            
            var detallesVenta = await _context.DetalleVentas
                .Include(dv => dv.Venta)
                .Include(dv => dv.Producto)
                    .ThenInclude(p => p!.Categoria)
                .Where(dv => dv.Venta.SucursalId == sucursalId &&
                           dv.Venta.FechaVenta.Date >= fechaDesdeUtc.Date &&
                           dv.Venta.FechaVenta.Date <= fechaHastaUtc.Date &&
                           dv.Producto != null)
                .ToListAsync();

            // Filtrar completadas en memoria
            var detallesCompletados = detallesVenta
                .Where(dv => dv.Venta.EstadoVenta == EstadoVenta.Completada)
                .ToList();

            var ventasPorCategoria = detallesCompletados
                .GroupBy(dv => dv.Producto!.Categoria.Nombre)
                .Select(g => new UtilidadLineaDto
                {
                    Linea = g.Key,
                    Ingresos = g.Sum(dv => dv.Subtotal),
                    CantidadProductos = g.Sum(dv => dv.Cantidad),
                    TicketPromedio = g.Count() > 0 ? g.Average(dv => dv.PrecioUnitario) : 0,
                    // Estimaciones de costos (en producción usar costos reales)
                    CostosEstimados = g.Sum(dv => dv.Subtotal) * 0.6m, // 60% costo estimado
                    UtilidadEstimada = g.Sum(dv => dv.Subtotal) * 0.4m, // 40% utilidad estimada
                    MargenPorcentaje = 40m
                })
                .OrderByDescending(u => u.Ingresos)
                .ToList();

            return ventasPorCategoria;
        }

        private async Task<List<DesperdicioDto>> GetDesperdicioMateriasPrimas(int sucursalId, DateTime fechaDesde, DateTime fechaHasta)
        {
            var fechaDesdeUtc = DateTime.SpecifyKind(fechaDesde, DateTimeKind.Utc);
            var fechaHastaUtc = DateTime.SpecifyKind(fechaHasta, DateTimeKind.Utc);
            
            var desperdicios = await _context.MovimientosInventario
                .Include(mi => mi.MateriaPrima)
                    .ThenInclude(mp => mp.Categoria)
                .Where(mi => mi.SucursalId == sucursalId &&
                           mi.FechaMovimiento.Date >= fechaDesdeUtc.Date &&
                           mi.FechaMovimiento.Date <= fechaHastaUtc.Date &&
                           mi.TipoMovimiento == TipoMovimiento.Merma)
                .GroupBy(mi => new { 
                    MateriaPrimaNombre = mi.MateriaPrima.Nombre,
                    CategoriaNombre = mi.MateriaPrima.Categoria.Nombre,
                    UnidadMedida = mi.MateriaPrima.UnidadMedida 
                })
                .Select(g => new DesperdicioDto
                {
                    MateriaPrima = g.Key.MateriaPrimaNombre,
                    Categoria = g.Key.CategoriaNombre,
                    UnidadMedida = g.Key.UnidadMedida,
                    CantidadDesperdiciada = g.Sum(mi => mi.Cantidad),
                    CostoDelDesperdicio = g.Sum(mi => mi.MontoTotal ?? 0),
                    MotivosPrincipales = string.Join(", ", g.Select(mi => mi.Motivo).Distinct().Take(3))
                })
                .OrderByDescending(d => d.CostoDelDesperdicio)
                .Take(10)
                .ToListAsync();

            // Calcular porcentajes
            var totalDesperdicio = desperdicios.Sum(d => d.CostoDelDesperdicio);
            foreach (var desperdicio in desperdicios)
            {
                desperdicio.PorcentajeDelTotal = totalDesperdicio > 0 ? 
                    desperdicio.CostoDelDesperdicio / totalDesperdicio * 100 : 0;
            }

            return desperdicios;
        }

        private async Task<MetricasInventarioDto> GetMetricasInventario(int sucursalId)
        {
            var fechaHoy = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
            var fechaInicioMes = DateTime.SpecifyKind(new DateTime(fechaHoy.Year, fechaHoy.Month, 1), DateTimeKind.Utc);

            var stocks = await _context.StockSucursal
                .Include(ss => ss.MateriaPrima)
                .Where(ss => ss.SucursalId == sucursalId)
                .ToListAsync();

            var movimientosHoy = await _context.MovimientosInventario
                .Where(mi => mi.SucursalId == sucursalId && mi.FechaMovimiento.Date == fechaHoy.Date)
                .CountAsync();

            var comprasDelMes = await _context.MovimientosInventario
                .Where(mi => mi.SucursalId == sucursalId && 
                           mi.FechaMovimiento.Date >= fechaInicioMes.Date &&
                           mi.TipoMovimiento == TipoMovimiento.Entrada)
                .SumAsync(mi => mi.MontoTotal ?? 0);

            var mermasDelMes = await _context.MovimientosInventario
                .Where(mi => mi.SucursalId == sucursalId && 
                           mi.FechaMovimiento.Date >= fechaInicioMes.Date &&
                           mi.TipoMovimiento == TipoMovimiento.Merma)
                .SumAsync(mi => mi.MontoTotal ?? 0);

            var alertas = stocks
                .Where(s => s.CantidadActual <= s.MateriaPrima.StockMinimo)
                .Take(5)
                .Select(s => new AlertaStockResumenDto
                {
                    MateriaPrima = s.MateriaPrima.Nombre,
                    TipoAlerta = s.CantidadActual <= 0 ? "AGOTADO" : "STOCK_BAJO",
                    CantidadActual = s.CantidadActual,
                    UnidadMedida = s.MateriaPrima.UnidadMedida,
                    DiasEstimadosAgotamiento = s.CantidadActual <= 0 ? 0 : 5 // Estimación simplificada
                })
                .ToList();

            return new MetricasInventarioDto
            {
                ValorTotalInventario = stocks.Sum(s => s.CantidadActual * s.MateriaPrima.CostoPromedio),
                MaterialesStockBajo = stocks.Count(s => s.CantidadActual <= s.MateriaPrima.StockMinimo && s.CantidadActual > 0),
                MaterialesAgotados = stocks.Count(s => s.CantidadActual <= 0),
                TotalMovimientosHoy = movimientosHoy,
                MontoComprasDelMes = comprasDelMes,
                MontoMermasDelMes = mermasDelMes,
                AlertasPrioritarias = alertas
            };
        }

        private async Task<List<VentaPorDiaDto>> GetVentasUltimos7Dias(int sucursalId)
        {
            return await GetVentasUltimosDias(sucursalId, 7);
        }

        private async Task<List<VentaPorDiaDto>> GetVentasUltimosDias(int sucursalId, int dias)
        {
            var fechaInicio = DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(-dias + 1), DateTimeKind.Utc);
            var fechaFin = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);

            var ventas = await _context.Ventas
                .Where(v => v.SucursalId == sucursalId &&
                           v.FechaVenta.Date >= fechaInicio.Date &&
                           v.FechaVenta.Date <= fechaFin.Date)
                .ToListAsync();

            // Filtrar completadas en memoria
            var ventasCompletadas = ventas
                .Where(v => v.EstadoVenta == EstadoVenta.Completada)
                .ToList();

            var ventasPorDia = ventasCompletadas
                .GroupBy(v => v.FechaVenta.Date)
                .Select(g => new VentaPorDiaDto
                {
                    Fecha = g.Key,
                    DiaNombre = g.Key.ToString("dddd", new System.Globalization.CultureInfo("es-ES")),
                    MontoVentas = g.Sum(v => v.Total),
                    CantidadTransacciones = g.Count(),
                    TicketPromedio = g.Count() > 0 ? g.Average(v => v.Total) : 0,
                    EsFestivo = false // En producción, verificar contra calendario de festivos
                })
                .OrderBy(v => v.Fecha)
                .ToList();

            // Completar días faltantes con ventas en 0
            var todasLasFechas = Enumerable.Range(0, dias)
                .Select(i => fechaInicio.Date.AddDays(i))
                .ToList();

            var ventasCompletas = todasLasFechas
                .GroupJoin(ventasPorDia,
                    fecha => fecha,
                    venta => venta.Fecha,
                    (fecha, ventas) => ventas.FirstOrDefault() ?? new VentaPorDiaDto
                    {
                        Fecha = fecha,
                        DiaNombre = fecha.ToString("dddd", new System.Globalization.CultureInfo("es-ES")),
                        MontoVentas = 0,
                        CantidadTransacciones = 0,
                        TicketPromedio = 0,
                        EsFestivo = false
                    })
                .OrderBy(v => v.Fecha)
                .ToList();

            return ventasCompletas;
        }

        private static string ObtenerPeriodoDelDia(int hora)
        {
            return hora switch
            {
                >= 6 and < 12 => "Mañana",
                >= 12 and < 18 => "Tarde",
                _ => "Noche"
            };
        }
    }
}