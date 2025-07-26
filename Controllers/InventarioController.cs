using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using LaCazuelaChapina.API.Data;
using LaCazuelaChapina.API.Models.Inventario;
using LaCazuelaChapina.API.Models.Enums;
using LaCazuelaChapina.API.DTOs.Inventario;

namespace LaCazuelaChapina.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class InventarioController : ControllerBase
    {
        private readonly CazuelaDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<InventarioController> _logger;

        public InventarioController(
            CazuelaDbContext context,
            IMapper mapper,
            ILogger<InventarioController> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene el stock actual de una sucursal
        /// </summary>
        [HttpGet("stock/sucursal/{sucursalId}")]
        [ProducesResponseType(typeof(List<StockSucursalDto>), 200)]
        public async Task<ActionResult<List<StockSucursalDto>>> GetStockSucursal(
            int sucursalId,
            [FromQuery] int? categoriaId = null,
            [FromQuery] string? estado = null)
        {
            try
            {
                var query = _context.StockSucursal
                    .Include(ss => ss.MateriaPrima)
                        .ThenInclude(mp => mp.Categoria)
                    .Include(ss => ss.Sucursal)
                    .Where(ss => ss.SucursalId == sucursalId);

                // Filtrar por categoría si se especifica
                if (categoriaId.HasValue)
                    query = query.Where(ss => ss.MateriaPrima.CategoriaId == categoriaId);

                // Filtrar por estado si se especifica
                if (!string.IsNullOrEmpty(estado))
                {
                    switch (estado.ToLower())
                    {
                        case "agotado":
                            query = query.Where(ss => ss.CantidadActual <= 0);
                            break;
                        case "bajo":
                            query = query.Where(ss => ss.CantidadActual > 0 && ss.CantidadActual <= ss.MateriaPrima.StockMinimo);
                            break;
                        case "ok":
                            query = query.Where(ss => ss.CantidadActual > ss.MateriaPrima.StockMinimo);
                            break;
                    }
                }

                var stocks = await query
                    .OrderBy(ss => ss.MateriaPrima.Categoria.Nombre)
                    .ThenBy(ss => ss.MateriaPrima.Nombre)
                    .ToListAsync();

                var stocksDto = stocks.Select(ss => 
                {
                    var dto = _mapper.Map<StockSucursalDto>(ss);
                    dto.EstadoStock = DeterminarEstadoStock(ss.CantidadActual, ss.MateriaPrima.StockMinimo);
                    return dto;
                }).ToList();

                _logger.LogInformation("✅ Se obtuvo stock de {Count} materias primas para sucursal {SucursalId}", 
                    stocksDto.Count, sucursalId);

                return Ok(stocksDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo stock de sucursal {SucursalId}", sucursalId);
                return StatusCode(500, new { 
                    message = "Error interno del servidor",
                    error = ex.Message 
                });
            }
        }

        /// <summary>
        /// Obtiene alertas de stock bajo para una sucursal
        /// </summary>
        [HttpGet("alertas/sucursal/{sucursalId}")]
        [ProducesResponseType(typeof(List<AlertaStockDto>), 200)]
        public async Task<ActionResult<List<AlertaStockDto>>> GetAlertasStock(int sucursalId)
        {
            try
            {
                var stocksBajos = await _context.StockSucursal
                    .Include(ss => ss.MateriaPrima)
                        .ThenInclude(mp => mp.Categoria)
                    .Include(ss => ss.Sucursal)
                    .Where(ss => ss.SucursalId == sucursalId && 
                               ss.CantidadActual <= ss.MateriaPrima.StockMinimo)
                    .OrderBy(ss => ss.CantidadActual / ss.MateriaPrima.StockMinimo)
                    .ToListAsync();

                var alertas = stocksBajos.Select(async ss => new AlertaStockDto
                {
                    MateriaPrimaId = ss.MateriaPrimaId,
                    MateriaPrimaNombre = ss.MateriaPrima.Nombre,
                    CategoriaNombre = ss.MateriaPrima.Categoria.Nombre,
                    CantidadActual = ss.CantidadActual,
                    StockMinimo = ss.MateriaPrima.StockMinimo,
                    UnidadMedida = ss.MateriaPrima.UnidadMedida,
                    PorcentajeStock = ss.MateriaPrima.StockMinimo > 0 ? 
                        (ss.CantidadActual / ss.MateriaPrima.StockMinimo) * 100 : 0,
                    TipoAlerta = ss.CantidadActual <= 0 ? "AGOTADO" : 
                                ss.CantidadActual <= (ss.MateriaPrima.StockMinimo * 0.2m) ? "CRITICO" : "STOCK_BAJO",
                    NivelPrioridad = ss.CantidadActual <= 0 ? "ALTA" : 
                                   ss.CantidadActual <= (ss.MateriaPrima.StockMinimo * 0.5m) ? "ALTA" : "MEDIA",
                    FechaDeteccion = DateTime.UtcNow,
                    DiasEstimadosAgotamiento = CalcularDiasAgotamiento(ss),
                    CostoReposicion = (ss.MateriaPrima.StockMaximo - ss.CantidadActual) * ss.MateriaPrima.CostoPromedio,
                    ProductosAfectados = await ObtenerProductosAfectados(ss.MateriaPrimaId)
                }).ToList();

                _logger.LogInformation("⚠️ Se encontraron {Count} alertas de stock para sucursal {SucursalId}", 
                    alertas.Count, sucursalId);

                return Ok(alertas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo alertas de stock para sucursal {SucursalId}", sucursalId);
                return StatusCode(500, new { 
                    message = "Error interno del servidor",
                    error = ex.Message 
                });
            }
        }

        /// <summary>
        /// Registra una entrada de inventario (compra)
        /// </summary>
        [HttpPost("entrada")]
        [ProducesResponseType(typeof(MovimientoInventarioDto), 201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<MovimientoInventarioDto>> RegistrarEntrada(RegistrarEntradaDto entradaDto)
        {
            try
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                // Validar que existe la materia prima y sucursal
                var stock = await _context.StockSucursal
                    .Include(ss => ss.MateriaPrima)
                        .ThenInclude(mp => mp.Categoria)
                    .Include(ss => ss.Sucursal)
                    .FirstOrDefaultAsync(ss => ss.SucursalId == entradaDto.SucursalId && 
                                             ss.MateriaPrimaId == entradaDto.MateriaPrimaId);

                if (stock == null)
                    return BadRequest(new { message = "Stock no encontrado para la sucursal y materia prima especificadas" });

                // Registrar movimiento de entrada
                var movimiento = _mapper.Map<MovimientoInventario>(entradaDto);
                movimiento.MontoTotal = entradaDto.Cantidad * entradaDto.CostoUnitario;

                _context.MovimientosInventario.Add(movimiento);

                // Actualizar stock actual
                var stockAnterior = stock.CantidadActual;
                stock.CantidadActual += entradaDto.Cantidad;
                stock.FechaUltimaActualizacion = DateTime.UtcNow;

                // Actualizar costo promedio (FIFO ponderado)
                if (stock.CantidadActual > 0)
                {
                    var costoTotalAnterior = stock.MateriaPrima.CostoPromedio * stockAnterior;
                    var costoTotalNuevo = entradaDto.CostoUnitario * entradaDto.Cantidad;
                    stock.MateriaPrima.CostoPromedio = (costoTotalAnterior + costoTotalNuevo) / stock.CantidadActual;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Obtener movimiento completo para respuesta
                var movimientoCompleto = await _context.MovimientosInventario
                    .Include(mi => mi.MateriaPrima)
                        .ThenInclude(mp => mp.Categoria)
                    .Include(mi => mi.Sucursal)
                    .FirstAsync(mi => mi.Id == movimiento.Id);

                var movimientoDto = _mapper.Map<MovimientoInventarioDto>(movimientoCompleto);
                movimientoDto.StockAnterior = stockAnterior;
                movimientoDto.StockActual = stock.CantidadActual;

                _logger.LogInformation("📦 Entrada registrada: {Cantidad} {UnidadMedida} de {MateriaPrima} por Q{Costo}", 
                    entradaDto.Cantidad, stock.MateriaPrima.UnidadMedida, stock.MateriaPrima.Nombre, movimiento.MontoTotal);

                return CreatedAtAction(nameof(GetMovimiento), new { id = movimiento.Id }, movimientoDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error registrando entrada de inventario");
                return StatusCode(500, new { 
                    message = "Error interno del servidor",
                    error = ex.Message 
                });
            }
        }

        /// <summary>
        /// Registra una salida de inventario
        /// </summary>
        [HttpPost("salida")]
        [ProducesResponseType(typeof(MovimientoInventarioDto), 201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<MovimientoInventarioDto>> RegistrarSalida(RegistrarSalidaDto salidaDto)
        {
            try
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                var stock = await _context.StockSucursal
                    .Include(ss => ss.MateriaPrima)
                        .ThenInclude(mp => mp.Categoria)
                    .Include(ss => ss.Sucursal)
                    .FirstOrDefaultAsync(ss => ss.SucursalId == salidaDto.SucursalId && 
                                             ss.MateriaPrimaId == salidaDto.MateriaPrimaId);

                if (stock == null)
                    return BadRequest(new { message = "Stock no encontrado" });

                if (stock.CantidadActual < salidaDto.Cantidad)
                    return BadRequest(new { message = "No hay suficiente stock para registrar la salida" });

                // Registrar movimiento de salida
                var movimiento = _mapper.Map<MovimientoInventario>(salidaDto);
                movimiento.CostoUnitario = stock.MateriaPrima.CostoPromedio;
                movimiento.MontoTotal = salidaDto.Cantidad * stock.MateriaPrima.CostoPromedio;

                _context.MovimientosInventario.Add(movimiento);

                // Actualizar stock actual
                var stockAnterior = stock.CantidadActual;
                stock.CantidadActual -= salidaDto.Cantidad;
                stock.FechaUltimaActualizacion = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var movimientoCompleto = await _context.MovimientosInventario
                    .Include(mi => mi.MateriaPrima)
                        .ThenInclude(mp => mp.Categoria)
                    .Include(mi => mi.Sucursal)
                    .FirstAsync(mi => mi.Id == movimiento.Id);

                var movimientoDto = _mapper.Map<MovimientoInventarioDto>(movimientoCompleto);
                movimientoDto.StockAnterior = stockAnterior;
                movimientoDto.StockActual = stock.CantidadActual;

                _logger.LogInformation("📤 Salida registrada: {Cantidad} {UnidadMedida} de {MateriaPrima}", 
                    salidaDto.Cantidad, stock.MateriaPrima.UnidadMedida, stock.MateriaPrima.Nombre);

                return CreatedAtAction(nameof(GetMovimiento), new { id = movimiento.Id }, movimientoDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error registrando salida de inventario");
                return StatusCode(500, new { 
                    message = "Error interno del servidor",
                    error = ex.Message 
                });
            }
        }

        /// <summary>
        /// Registra una merma de inventario
        /// </summary>
        [HttpPost("merma")]
        [ProducesResponseType(typeof(MovimientoInventarioDto), 201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<MovimientoInventarioDto>> RegistrarMerma(RegistrarMermaDto mermaDto)
        {
            try
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                var stock = await _context.StockSucursal
                    .Include(ss => ss.MateriaPrima)
                        .ThenInclude(mp => mp.Categoria)
                    .Include(ss => ss.Sucursal)
                    .FirstOrDefaultAsync(ss => ss.SucursalId == mermaDto.SucursalId && 
                                             ss.MateriaPrimaId == mermaDto.MateriaPrimaId);

                if (stock == null)
                    return BadRequest(new { message = "Stock no encontrado" });

                if (stock.CantidadActual < mermaDto.Cantidad)
                    return BadRequest(new { message = "No hay suficiente stock para registrar la merma" });

                // Registrar movimiento de merma
                var movimiento = _mapper.Map<MovimientoInventario>(mermaDto);
                movimiento.CostoUnitario = stock.MateriaPrima.CostoPromedio;
                movimiento.MontoTotal = mermaDto.Cantidad * stock.MateriaPrima.CostoPromedio;

                // Agregar observaciones si existen
                if (!string.IsNullOrEmpty(mermaDto.Observaciones))
                {
                    movimiento.Motivo += $" | Observaciones: {mermaDto.Observaciones}";
                }

                _context.MovimientosInventario.Add(movimiento);

                // Actualizar stock actual
                var stockAnterior = stock.CantidadActual;
                stock.CantidadActual -= mermaDto.Cantidad;
                stock.FechaUltimaActualizacion = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // TODO: Si requiere investigación, crear alerta o notificación

                var movimientoCompleto = await _context.MovimientosInventario
                    .Include(mi => mi.MateriaPrima)
                        .ThenInclude(mp => mp.Categoria)
                    .Include(mi => mi.Sucursal)
                    .FirstAsync(mi => mi.Id == movimiento.Id);

                var movimientoDto = _mapper.Map<MovimientoInventarioDto>(movimientoCompleto);
                movimientoDto.StockAnterior = stockAnterior;
                movimientoDto.StockActual = stock.CantidadActual;

                _logger.LogInformation("🗑️ Merma registrada: {Cantidad} {UnidadMedida} de {MateriaPrima}. Tipo: {TipoMerma}", 
                    mermaDto.Cantidad, stock.MateriaPrima.UnidadMedida, stock.MateriaPrima.Nombre, mermaDto.TipoMerma);

                return CreatedAtAction(nameof(GetMovimiento), new { id = movimiento.Id }, movimientoDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error registrando merma de inventario");
                return StatusCode(500, new { 
                    message = "Error interno del servidor",
                    error = ex.Message 
                });
            }
        }

        /// <summary>
        /// Registra un ajuste de inventario
        /// </summary>
        [HttpPost("ajuste")]
        [ProducesResponseType(typeof(MovimientoInventarioDto), 201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<MovimientoInventarioDto>> RegistrarAjuste(RegistrarAjusteDto ajusteDto)
        {
            try
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                var stock = await _context.StockSucursal
                    .Include(ss => ss.MateriaPrima)
                        .ThenInclude(mp => mp.Categoria)
                    .Include(ss => ss.Sucursal)
                    .FirstOrDefaultAsync(ss => ss.SucursalId == ajusteDto.SucursalId && 
                                             ss.MateriaPrimaId == ajusteDto.MateriaPrimaId);

                if (stock == null)
                    return BadRequest(new { message = "Stock no encontrado" });

                if (ajusteDto.CantidadAnterior != stock.CantidadActual)
                    return BadRequest(new { message = "La cantidad anterior no coincide con el stock actual" });

                // Registrar movimiento de ajuste
                var movimiento = _mapper.Map<MovimientoInventario>(ajusteDto);
                movimiento.CostoUnitario = stock.MateriaPrima.CostoPromedio;
                movimiento.MontoTotal = Math.Abs(ajusteDto.CantidadNueva - ajusteDto.CantidadAnterior) * stock.MateriaPrima.CostoPromedio;

                _context.MovimientosInventario.Add(movimiento);

                // Actualizar stock actual
                var stockAnterior = stock.CantidadActual;
                stock.CantidadActual = ajusteDto.CantidadNueva;
                stock.FechaUltimaActualizacion = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var movimientoCompleto = await _context.MovimientosInventario
                    .Include(mi => mi.MateriaPrima)
                        .ThenInclude(mp => mp.Categoria)
                    .Include(mi => mi.Sucursal)
                    .FirstAsync(mi => mi.Id == movimiento.Id);

                var movimientoDto = _mapper.Map<MovimientoInventarioDto>(movimientoCompleto);
                movimientoDto.StockAnterior = stockAnterior;
                movimientoDto.StockActual = stock.CantidadActual;

                _logger.LogInformation("⚖️ Ajuste registrado: {MateriaPrima} de {StockAnterior} a {StockNuevo} {UnidadMedida}", 
                    stock.MateriaPrima.Nombre, stockAnterior, stock.CantidadActual, stock.MateriaPrima.UnidadMedida);

                return CreatedAtAction(nameof(GetMovimiento), new { id = movimiento.Id }, movimientoDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error registrando ajuste de inventario");
                return StatusCode(500, new { 
                    message = "Error interno del servidor",
                    error = ex.Message 
                });
            }
        }

        /// <summary>
        /// Obtiene un movimiento de inventario por ID
        /// </summary>
        [HttpGet("movimiento/{id}")]
        [ProducesResponseType(typeof(MovimientoInventarioDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<MovimientoInventarioDto>> GetMovimiento(int id)
        {
            try
            {
                var movimiento = await _context.MovimientosInventario
                    .Include(mi => mi.MateriaPrima)
                        .ThenInclude(mp => mp.Categoria)
                    .Include(mi => mi.Sucursal)
                    .FirstOrDefaultAsync(mi => mi.Id == id);

                if (movimiento == null)
                    return NotFound(new { message = "Movimiento no encontrado" });

                var movimientoDto = _mapper.Map<MovimientoInventarioDto>(movimiento);
                return Ok(movimientoDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo movimiento {Id}", id);
                return StatusCode(500, new { 
                    message = "Error interno del servidor",
                    error = ex.Message 
                });
            }
        }

        /// <summary>
        /// Obtiene historial de movimientos con filtros
        /// </summary>
        [HttpGet("movimientos")]
        [ProducesResponseType(typeof(List<MovimientoInventarioDto>), 200)]
        public async Task<ActionResult<List<MovimientoInventarioDto>>> GetMovimientos(
            [FromQuery] FiltroInventarioDto filtro)
        {
            try
            {
                var query = _context.MovimientosInventario
                    .Include(mi => mi.MateriaPrima)
                        .ThenInclude(mp => mp.Categoria)
                    .Include(mi => mi.Sucursal)
                    .AsQueryable();

                // Aplicar filtros
                if (filtro.SucursalId.HasValue)
                    query = query.Where(mi => mi.SucursalId == filtro.SucursalId);

                if (filtro.CategoriaId.HasValue)
                    query = query.Where(mi => mi.MateriaPrima.CategoriaId == filtro.CategoriaId);

                if (filtro.TipoMovimiento.HasValue)
                    query = query.Where(mi => mi.TipoMovimiento == filtro.TipoMovimiento);

                if (filtro.FechaDesde.HasValue)
                    query = query.Where(mi => mi.FechaMovimiento.Date >= filtro.FechaDesde.Value.Date);

                if (filtro.FechaHasta.HasValue)
                    query = query.Where(mi => mi.FechaMovimiento.Date <= filtro.FechaHasta.Value.Date);

                if (!string.IsNullOrEmpty(filtro.TextoBusqueda))
                    query = query.Where(mi => mi.MateriaPrima.Nombre.Contains(filtro.TextoBusqueda) ||
                                            mi.Motivo!.Contains(filtro.TextoBusqueda));

                // Ordenar
                query = filtro.OrdenarPor?.ToLower() switch
                {
                    "nombre" => filtro.OrdenDescendente 
                        ? query.OrderByDescending(mi => mi.MateriaPrima.Nombre)
                        : query.OrderBy(mi => mi.MateriaPrima.Nombre),
                    "cantidad" => filtro.OrdenDescendente 
                        ? query.OrderByDescending(mi => mi.Cantidad)
                        : query.OrderBy(mi => mi.Cantidad),
                    "valor" => filtro.OrdenDescendente 
                        ? query.OrderByDescending(mi => mi.MontoTotal)
                        : query.OrderBy(mi => mi.MontoTotal),
                    _ => query.OrderByDescending(mi => mi.FechaMovimiento)
                };

                var movimientos = await query
                    .Take(100) // Limitar resultados
                    .ToListAsync();

                var movimientosDto = _mapper.Map<List<MovimientoInventarioDto>>(movimientos);

                _logger.LogInformation("📋 Se obtuvieron {Count} movimientos de inventario", movimientosDto.Count);

                return Ok(movimientosDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo movimientos de inventario");
                return StatusCode(500, new { 
                    message = "Error interno del servidor",
                    error = ex.Message 
                });
            }
        }

        /// <summary>
        /// Obtiene resumen de inventario por sucursal
        /// </summary>
        [HttpGet("resumen/{sucursalId}")]
        [ProducesResponseType(typeof(ResumenInventarioDto), 200)]
        public async Task<ActionResult<ResumenInventarioDto>> GetResumenInventario(int sucursalId)
        {
            try
            {
                var sucursal = await _context.Sucursales.FindAsync(sucursalId);
                if (sucursal == null)
                    return NotFound(new { message = "Sucursal no encontrada" });

                var fechaHoy = DateTime.UtcNow.Date;
                var fechaInicioMes = new DateTime(fechaHoy.Year, fechaHoy.Month, 1);

                // Calcular métricas básicas
                var stocks = await _context.StockSucursal
                    .Include(ss => ss.MateriaPrima)
                        .ThenInclude(mp => mp.Categoria)
                    .Where(ss => ss.SucursalId == sucursalId)
                    .ToListAsync();

                var totalMateriasPrimas = stocks.Count;
                var valorTotalInventario = stocks.Sum(s => s.CantidadActual * s.MateriaPrima.CostoPromedio);
                var materialesStockBajo = stocks.Count(s => s.CantidadActual <= s.MateriaPrima.StockMinimo && s.CantidadActual > 0);
                var materialesAgotados = stocks.Count(s => s.CantidadActual <= 0);

                // Movimientos del día
                var movimientosHoy = await _context.MovimientosInventario
                    .Where(mi => mi.SucursalId == sucursalId && mi.FechaMovimiento.Date == fechaHoy)
                    .CountAsync();

                // Mermas del mes
                var montoMermasDelMes = await _context.MovimientosInventario
                    .Where(mi => mi.SucursalId == sucursalId && 
                               mi.FechaMovimiento >= fechaInicioMes &&
                               mi.TipoMovimiento == TipoMovimiento.Merma)
                    .SumAsync(mi => mi.MontoTotal ?? 0);

                // Alertas prioritarias
                var alertas = await GetAlertasStock(sucursalId);
                var alertasDto = alertas.Value as List<AlertaStockDto> ?? new List<AlertaStockDto>();

                // Materiales más consumidos
                var materialesMasConsumidos = await _context.MovimientosInventario
                    .Include(mi => mi.MateriaPrima)
                    .Where(mi => mi.SucursalId == sucursalId && 
                               mi.FechaMovimiento >= fechaInicioMes &&
                               mi.TipoMovimiento == TipoMovimiento.Salida)
                    .GroupBy(mi => new { mi.MateriaPrimaId, mi.MateriaPrima.Nombre, mi.MateriaPrima.UnidadMedida })
                    .Select(g => new MaterialMasConsumidoDto
                    {
                        MateriaPrimaNombre = g.Key.Nombre,
                        CantidadConsumida = g.Sum(mi => mi.Cantidad),
                        UnidadMedida = g.Key.UnidadMedida,
                        CostoTotal = g.Sum(mi => mi.MontoTotal ?? 0),
                        PorcentajeDelTotal = 0 // Se calculará después
                    })
                    .OrderByDescending(m => m.CantidadConsumida)
                    .Take(5)
                    .ToListAsync();

                var resumen = new ResumenInventarioDto
                {
                    SucursalNombre = sucursal.Nombre,
                    TotalMateriasPrimas = totalMateriasPrimas,
                    ValorTotalInventario = valorTotalInventario,
                    MaterialesStockBajo = materialesStockBajo,
                    MaterialesAgotados = materialesAgotados,
                    MovimientosHoy = movimientosHoy,
                    MontoMermasDelMes = montoMermasDelMes,
                    AlertasPrioritarias = alertasDto.Take(5).ToList(),
                    MaterialesMasConsumidos = materialesMasConsumidos
                };

                _logger.LogInformation("📊 Resumen de inventario generado para sucursal {SucursalId}", sucursalId);

                return Ok(resumen);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error generando resumen de inventario para sucursal {SucursalId}", sucursalId);
                return StatusCode(500, new { 
                    message = "Error interno del servidor",
                    error = ex.Message 
                });
            }
        }

        /// <summary>
        /// Obtiene materias primas disponibles
        /// </summary>
        [HttpGet("materias-primas")]
        [ProducesResponseType(typeof(List<MateriaPrimaDto>), 200)]
        public async Task<ActionResult<List<MateriaPrimaDto>>> GetMateriasPrimas(
            [FromQuery] int? categoriaId = null)
        {
            try
            {
                var query = _context.MateriasPrimas
                    .Include(mp => mp.Categoria)
                    .Include(mp => mp.Stocks)
                        .ThenInclude(s => s.Sucursal)
                    .Where(mp => mp.Activa);

                if (categoriaId.HasValue)
                    query = query.Where(mp => mp.CategoriaId == categoriaId);

                var materiasPrimas = await query
                    .OrderBy(mp => mp.Categoria.Nombre)
                    .ThenBy(mp => mp.Nombre)
                    .ToListAsync();

                var materiasPrimasDto = _mapper.Map<List<MateriaPrimaDto>>(materiasPrimas);

                _logger.LogInformation("📋 Se obtuvieron {Count} materias primas", materiasPrimasDto.Count);

                return Ok(materiasPrimasDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo materias primas");
                return StatusCode(500, new { 
                    message = "Error interno del servidor",
                    error = ex.Message 
                });
            }
        }

        // =============================================
        // MÉTODOS PRIVADOS
        // =============================================

        private static string DeterminarEstadoStock(decimal cantidadActual, decimal stockMinimo)
        {
            if (cantidadActual <= 0)
                return "🔴 AGOTADO";
            else if (cantidadActual <= stockMinimo * 0.2m)
                return "🟠 CRÍTICO";
            else if (cantidadActual <= stockMinimo)
                return "🟡 REORDENAR";
            else
                return "🟢 OK";
        }

        private int CalcularDiasAgotamiento(StockSucursal stock)
        {
            // Cálculo simplificado - en producción usar historial real de consumo
            var consumoPromedioDiario = 2.0m; // Placeholder
            return stock.CantidadActual <= 0 ? 0 : (int)(stock.CantidadActual / consumoPromedioDiario);
        }

        private async Task<List<string>> ObtenerProductosAfectados(int materiaPrimaId)
        {
            // Simplificado - en producción hacer consulta real basada en recetas/BOM
            return new List<string> { "Productos relacionados" };
        }
    }
}