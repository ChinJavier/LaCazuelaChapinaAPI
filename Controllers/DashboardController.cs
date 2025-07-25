// =============================================
// ARCHIVO: Controllers/DashboardController.cs
// Controlador para dashboard e indicadores clave
// =============================================

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaCazuelaChapina.API.Data;
using LaCazuelaChapina.API.Models.Enums;
using LaCazuelaChapina.API.DTOs.Dashboard;

namespace LaCazuelaChapina.API.Controllers
{
    /// <summary>
    /// Controlador para dashboard e indicadores de negocio
    /// </summary>
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
        /// <param name="sucursalId">ID de la sucursal</param>
        /// <returns>Datos completos del dashboard</returns>
        [HttpGet("sucursal/{sucursalId}")]
        [ProducesResponseType(typeof(DashboardDto), 200)]
        public async Task<ActionResult<DashboardDto>> GetDashboard(int sucursalId)
        {
            try
            {
                var fechaHoy = DateTime.UtcNow.Date;
                var fechaInicioMes = new DateTime(fechaHoy.Year, fechaHoy.Month, 1);

                var dashboard = new DashboardDto
                {
                    VentasDiarias = await GetVentasDiarias(sucursalId, fechaHoy),
                    VentasMensuales = await GetVentasMensuales(sucursalId, fechaInicioMes),
                    TamalesMasVendidos = await GetTamalesMasVendidos(sucursalId, fechaInicioMes),
                    BebidasPorHorario = await GetBebidasPorHorario(sucursalId, fechaHoy),
                    ProporcionPicante = await GetProporcionPicante(sucursalId, fechaInicioMes),
                    UtilidadesPorLinea = await GetUtilidadesPorLinea(sucursalId, fechaInicioMes),
                    DesperdicioMateriasPrimas = await GetDesperdicioMateriasPrimas(sucursalId, fechaInicioMes)
                };

                _logger.LogInformation("Dashboard generado para sucursal {SucursalId}", sucursalId);
                return Ok(dashboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando dashboard para sucursal {SucursalId}", sucursalId);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtiene ventas diarias de una sucursal
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
                _logger.LogError(ex, "Error obteniendo ventas diarias");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtiene los tamales más vendidos
        /// </summary>
        [HttpGet("tamales-mas-vendidos/{sucursalId}")]
        [ProducesResponseType(typeof(List<ProductoVendidoDto>), 200)]
        public async Task<ActionResult<List<ProductoVendidoDto>>> GetTamalesMasVendidosEndpoint(int sucursalId)
        {
            try
            {
                var fechaInicioMes = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                var tamales = await GetTamalesMasVendidos(sucursalId, fechaInicioMes);
                return Ok(tamales);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo tamales más vendidos");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtiene bebidas por horario
        /// </summary>
        [HttpGet("bebidas-por-horario/{sucursalId}")]
        [ProducesResponseType(typeof(List<BebidaPorHorarioDto>), 200)]
        public async Task<ActionResult<List<BebidaPorHorarioDto>>> GetBebidasPorHorarioEndpoint(int sucursalId)
        {
            try
            {
                var fechaHoy = DateTime.UtcNow.Date;
                var bebidas = await GetBebidasPorHorario(sucursalId, fechaHoy);
                return Ok(bebidas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo bebidas por horario");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtiene proporción de picante vs no picante
        /// </summary>
        [HttpGet("proporcion-picante/{sucursalId}")]
        [ProducesResponseType(typeof(ProporcionPicanteDto), 200)]
        public async Task<ActionResult<ProporcionPicanteDto>> GetProporcionPicanteEndpoint(int sucursalId)
        {
            try
            {
                var fechaInicioMes = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                var proporcion = await GetProporcionPicante(sucursalId, fechaInicioMes);
                return Ok(proporcion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo proporción de picante");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        // =============================================
        // MÉTODOS PRIVADOS PARA CÁLCULOS
        // =============================================

        private async Task<decimal> GetVentasDiarias(int sucursalId, DateTime fecha)
        {
            return await _context.Ventas
                .Where(v => v.SucursalId == sucursalId &&
                           v.FechaVenta.Date == fecha &&
                           v.EstadoVenta == EstadoVenta.Completada)
                .SumAsync(v => v.Total);
        }

        private async Task<decimal> GetVentasMensuales(int sucursalId, DateTime fechaInicioMes)
        {
            return await _context.Ventas
                .Where(v => v.SucursalId == sucursalId &&
                           v.FechaVenta >= fechaInicioMes &&
                           v.EstadoVenta == EstadoVenta.Completada)
                .SumAsync(v => v.Total);
        }

        private async Task<List<ProductoVendidoDto>> GetTamalesMasVendidos(int sucursalId, DateTime fechaDesde)
        {
            var tamales = await _context.DetalleVentas
                .Include(dv => dv.Venta)
                .Include(dv => dv.Producto)
                .Include(dv => dv.VarianteProducto)
                .Where(dv => dv.Venta.SucursalId == sucursalId &&
                           dv.Venta.FechaVenta >= fechaDesde &&
                           dv.Venta.EstadoVenta == EstadoVenta.Completada &&
                           dv.Producto!.Categoria.Nombre == "Tamales")
                .GroupBy(dv => new
                {
                    ProductoId = dv.ProductoId,
                    ProductoNombre = dv.Producto!.Nombre,
                    VarianteNombre = dv.VarianteProducto!.Nombre
                })
                .Select(g => new ProductoVendidoDto
                {
                    Nombre = $"{g.Key.ProductoNombre} ({g.Key.VarianteNombre})",
                    CantidadVendida = g.Sum(dv => dv.Cantidad),
                    MontoTotal = g.Sum(dv => dv.Subtotal)
                })
                .OrderByDescending(p => p.CantidadVendida)
                .Take(10)
                .ToListAsync();

            return tamales;
        }

        private async Task<List<BebidaPorHorarioDto>> GetBebidasPorHorario(int sucursalId, DateTime fecha)
        {
            var bebidasPorHora = await _context.DetalleVentas
                .Include(dv => dv.Venta)
                .Include(dv => dv.Personalizaciones)
                    .ThenInclude(pv => pv.OpcionAtributo)
                .Where(dv => dv.Venta.SucursalId == sucursalId &&
                           dv.Venta.FechaVenta.Date == fecha &&
                           dv.Venta.EstadoVenta == EstadoVenta.Completada &&
                           dv.Producto!.Categoria.Nombre == "Bebidas")
                .GroupBy(dv => new
                {
                    Hora = dv.Venta.FechaVenta.Hour,
                    TipoBebida = dv.Personalizaciones
                        .FirstOrDefault(p => p.TipoAtributo.Nombre == "Tipo Bebida")!
                        .OpcionAtributo.Nombre
                })
                .Select(g => new BebidaPorHorarioDto
                {
                    Hora = g.Key.Hora,
                    TipoBebida = g.Key.TipoBebida,
                    CantidadVendida = g.Sum(dv => dv.Cantidad)
                })
                .OrderBy(b => b.Hora)
                .ThenByDescending(b => b.CantidadVendida)
                .ToListAsync();

            return bebidasPorHora;
        }

        private async Task<ProporcionPicanteDto> GetProporcionPicante(int sucursalId, DateTime fechaDesde)
        {
            var tamalesConPicante = await _context.DetalleVentas
                .Include(dv => dv.Venta)
                .Include(dv => dv.Personalizaciones)
                    .ThenInclude(pv => pv.OpcionAtributo)
                .Where(dv => dv.Venta.SucursalId == sucursalId &&
                           dv.Venta.FechaVenta >= fechaDesde &&
                           dv.Venta.EstadoVenta == EstadoVenta.Completada &&
                           dv.Producto!.Categoria.Nombre == "Tamales")
                .SelectMany(dv => dv.Personalizaciones)
                .Where(pv => pv.TipoAtributo.Nombre == "Picante")
                .GroupBy(pv => pv.OpcionAtributo.Nombre)
                .Select(g => new
                {
                    Opcion = g.Key,
                    Cantidad = g.Sum(pv => pv.DetalleVenta.Cantidad)
                })
                .ToListAsync();

            var totalConPicante = tamalesConPicante
                .Where(t => t.Opcion != "Sin Chile")
                .Sum(t => t.Cantidad);

            var totalSinPicante = tamalesConPicante
                .Where(t => t.Opcion == "Sin Chile")
                .Sum(t => t.Cantidad);

            var totalTamales = totalConPicante + totalSinPicante;

            return new ProporcionPicanteDto
            {
                TotalConPicante = totalConPicante,
                TotalSinPicante = totalSinPicante,
                PorcentajeConPicante = totalTamales > 0 ? (decimal)totalConPicante / totalTamales * 100 : 0
            };
        }

        private async Task<List<UtilidadLineaDto>> GetUtilidadesPorLinea(int sucursalId, DateTime fechaDesde)
        {
            var utilidades = await _context.DetalleVentas
                .Include(dv => dv.Venta)
                .Include(dv => dv.Producto)
                    .ThenInclude(p => p!.Categoria)
                .Where(dv => dv.Venta.SucursalId == sucursalId &&
                           dv.Venta.FechaVenta >= fechaDesde &&
                           dv.Venta.EstadoVenta == EstadoVenta.Completada)
                .GroupBy(dv => dv.Producto!.Categoria.Nombre)
                .Select(g => new UtilidadLineaDto
                {
                    Linea = g.Key,
                    Ingresos = g.Sum(dv => dv.Subtotal),
                    Costos = g.Sum(dv => dv.Subtotal) * 0.6m, // Estimación del 60% como costo
                    Utilidad = g.Sum(dv => dv.Subtotal) * 0.4m, // 40% de utilidad estimada
                    MargenPorcentaje = 40m
                })
                .ToListAsync();

            return utilidades;
        }

        private async Task<List<DesperdicioDto>> GetDesperdicioMateriasPrimas(int sucursalId, DateTime fechaDesde)
        {
            var desperdicios = await _context.MovimientosInventario
                .Include(mi => mi.MateriaPrima)
                .Where(mi => mi.SucursalId == sucursalId &&
                           mi.FechaMovimiento >= fechaDesde &&
                           mi.TipoMovimiento == TipoMovimiento.Merma)
                .GroupBy(mi => new
                {
                    mi.MateriaPrima.Nombre,
                    mi.MateriaPrima.UnidadMedida
                })
                .Select(g => new DesperdicioDto
                {
                    MateriaPrima = g.Key.Nombre,
                    UnidadMedida = g.Key.UnidadMedida,
                    CantidadDesperdiciada = g.Sum(mi => mi.Cantidad),
                    CostoDelDesperdicio = g.Sum(mi => mi.MontoTotal ?? 0)
                })
                .OrderByDescending(d => d.CostoDelDesperdicio)
                .Take(10)
                .ToListAsync();

            return desperdicios;
        }
    }
}