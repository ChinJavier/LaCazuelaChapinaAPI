// =============================================
// ARCHIVO: Controllers/CombosController.cs
// Gestión de Combos - La Cazuela Chapina
// =============================================

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using LaCazuelaChapina.API.Data;
using LaCazuelaChapina.API.Models.Combos;
using LaCazuelaChapina.API.Models.Enums;
using LaCazuelaChapina.API.DTOs.Combos;

namespace LaCazuelaChapina.API.Controllers
{
    /// <summary>
    /// Controlador para la gestión de combos fijos y estacionales
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class CombosController : ControllerBase
    {
        private readonly CazuelaDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<CombosController> _logger;

        public CombosController(
            CazuelaDbContext context,
            IMapper mapper,
            ILogger<CombosController> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        // =============================================
        // CONSULTAS GENERALES
        // =============================================

        /// <summary>
        /// Obtiene todos los combos disponibles (activos y vigentes)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<ComboDto>>> GetCombosDisponibles()
        {
            try
            {
                var fechaActual = DateTime.UtcNow.Date;
                
                var combos = await _context.Combos
                    .Where(c => c.Activo && 
                        (c.TipoCombo == TipoCombo.Fijo || 
                         (c.TipoCombo == TipoCombo.Estacional && 
                          c.FechaInicioVigencia <= fechaActual && 
                          c.FechaFinVigencia >= fechaActual)))
                    .Include(c => c.Componentes)
                        .ThenInclude(cc => cc.Producto)
                    .Include(c => c.Componentes)
                        .ThenInclude(cc => cc.VarianteProducto)
                    .OrderBy(c => c.TipoCombo)
                    .ThenBy(c => c.Nombre)
                    .ToListAsync();

                var combosDto = _mapper.Map<List<ComboDto>>(combos);
                return Ok(combosDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener combos disponibles");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtiene todos los combos (incluye inactivos y fuera de vigencia)
        /// </summary>
        [HttpGet("todos")]
        public async Task<ActionResult<List<ComboDetalleDto>>> GetTodosCombos(
            [FromQuery] TipoCombo? tipoCombo = null,
            [FromQuery] bool? activo = null)
        {
            try
            {
                var query = _context.Combos
                    .Include(c => c.Componentes)
                        .ThenInclude(cc => cc.Producto)
                    .Include(c => c.Componentes)
                        .ThenInclude(cc => cc.VarianteProducto)
                    .Include(c => c.DetalleVentas)
                    .AsQueryable();

                if (tipoCombo.HasValue)
                {
                    query = query.Where(c => c.TipoCombo == tipoCombo.Value);
                }

                if (activo.HasValue)
                {
                    query = query.Where(c => c.Activo == activo.Value);
                }

                var combos = await query
                    .OrderBy(c => c.TipoCombo)
                    .ThenByDescending(c => c.FechaCreacion)
                    .ToListAsync();

                var combosDetalle = combos.Select(c => {
                    var dto = _mapper.Map<ComboDetalleDto>(c);
                    dto.VecesVendido = c.DetalleVentas?.Count ?? 0;
                    dto.IngresosGenerados = c.DetalleVentas?.Sum(dv => dv.Subtotal) ?? 0;
                    dto.EstaVigente = c.TipoCombo == TipoCombo.Fijo || 
                        (c.FechaInicioVigencia <= DateTime.UtcNow.Date && 
                         c.FechaFinVigencia >= DateTime.UtcNow.Date);
                    return dto;
                }).ToList();

                return Ok(combosDetalle);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener todos los combos");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtiene un combo específico por ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ComboDetalleDto>> GetCombo(int id)
        {
            try
            {
                var combo = await _context.Combos
                    .Include(c => c.Componentes)
                        .ThenInclude(cc => cc.Producto)
                            .ThenInclude(p => p.Categoria)
                    .Include(c => c.Componentes)
                        .ThenInclude(cc => cc.VarianteProducto)
                    .Include(c => c.DetalleVentas)
                        .ThenInclude(dv => dv.Venta)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (combo == null)
                {
                    return NotFound(new { message = $"Combo con ID {id} no encontrado" });
                }

                var comboDetalle = _mapper.Map<ComboDetalleDto>(combo);
                comboDetalle.VecesVendido = combo.DetalleVentas?.Count ?? 0;
                comboDetalle.IngresosGenerados = combo.DetalleVentas?.Sum(dv => dv.Subtotal) ?? 0;
                comboDetalle.EstaVigente = combo.TipoCombo == TipoCombo.Fijo || 
                    (combo.FechaInicioVigencia <= DateTime.UtcNow.Date && 
                     combo.FechaFinVigencia >= DateTime.UtcNow.Date);
                comboDetalle.UltimaVenta = combo.DetalleVentas?
                    .OrderByDescending(dv => dv.Venta.FechaVenta)
                    .FirstOrDefault()?.Venta.FechaVenta;

                return Ok(comboDetalle);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener combo {Id}", id);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtiene combos estacionales vigentes o próximos
        /// </summary>
        [HttpGet("estacionales")]
        public async Task<ActionResult<List<ComboEstacionalDto>>> GetCombosEstacionales()
        {
            try
            {
                var fechaActual = DateTime.UtcNow.Date;
                var fechaLimite = fechaActual.AddMonths(3); // Próximos 3 meses

                var combosEstacionales = await _context.Combos
                    .Where(c => c.TipoCombo == TipoCombo.Estacional && 
                        c.Activo &&
                        c.FechaFinVigencia >= fechaActual &&
                        c.FechaInicioVigencia <= fechaLimite)
                    .Include(c => c.Componentes)
                        .ThenInclude(cc => cc.Producto)
                    .Include(c => c.Componentes)
                        .ThenInclude(cc => cc.VarianteProducto)
                    .OrderBy(c => c.FechaInicioVigencia)
                    .ToListAsync();

                var combosDto = combosEstacionales.Select(c => {
                    var dto = _mapper.Map<ComboEstacionalDto>(c);
                    dto.DiasRestantes = c.FechaInicioVigencia > fechaActual ? 
                        (c.FechaInicioVigencia.Value - fechaActual).Days : 
                        (c.FechaFinVigencia.Value - fechaActual).Days;
                    dto.EstadoVigencia = c.FechaInicioVigencia > fechaActual ? "Próximo" :
                        c.FechaFinVigencia >= fechaActual ? "Vigente" : "Expirado";
                    return dto;
                }).ToList();

                return Ok(combosDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener combos estacionales");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        // =============================================
        // GESTIÓN DE COMBOS
        // =============================================

        /// <summary>
        /// Crea un nuevo combo
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ComboDto>> CrearCombo(CrearComboDto crearComboDto)
        {
            try
            {
                // Validar fechas para combos estacionales
                if (crearComboDto.TipoCombo == TipoCombo.Estacional)
                {
                    if (!crearComboDto.FechaInicioVigencia.HasValue || !crearComboDto.FechaFinVigencia.HasValue)
                    {
                        return BadRequest(new { message = "Los combos estacionales requieren fechas de vigencia" });
                    }

                    if (crearComboDto.FechaInicioVigencia >= crearComboDto.FechaFinVigencia)
                    {
                        return BadRequest(new { message = "La fecha de inicio debe ser anterior a la fecha de fin" });
                    }
                }

                // Verificar que no exista un combo con el mismo nombre
                var existeCombo = await _context.Combos
                    .AnyAsync(c => c.Nombre.ToLower() == crearComboDto.Nombre.ToLower() && c.Activo);

                if (existeCombo)
                {
                    return Conflict(new { message = "Ya existe un combo activo con ese nombre" });
                }

                var combo = _mapper.Map<Combo>(crearComboDto);
                combo.FechaCreacion = DateTime.UtcNow;
                combo.Activo = true;

                _context.Combos.Add(combo);
                await _context.SaveChangesAsync();

                // Agregar componentes si se especificaron
                if (crearComboDto.Componentes?.Any() == true)
                {
                    await AgregarComponentesCombo(combo.Id, crearComboDto.Componentes);
                }

                // Recargar con componentes
                await _context.Entry(combo)
                    .Collection(c => c.Componentes)
                    .Query()
                    .Include(cc => cc.Producto)
                    .Include(cc => cc.VarianteProducto)
                    .LoadAsync();

                var comboDto = _mapper.Map<ComboDto>(combo);
                
                _logger.LogInformation("Combo {Nombre} creado con ID {Id}", combo.Nombre, combo.Id);
                
                return CreatedAtAction(nameof(GetCombo), new { id = combo.Id }, comboDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear combo");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Actualiza un combo existente (clave para edición sin redeploy)
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> ActualizarCombo(int id, ActualizarComboDto actualizarComboDto)
        {
            try
            {
                var combo = await _context.Combos
                    .Include(c => c.Componentes)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (combo == null)
                {
                    return NotFound(new { message = $"Combo con ID {id} no encontrado" });
                }

                // Validar fechas para combos estacionales
                if (combo.TipoCombo == TipoCombo.Estacional)
                {
                    if (!actualizarComboDto.FechaInicioVigencia.HasValue || !actualizarComboDto.FechaFinVigencia.HasValue)
                    {
                        return BadRequest(new { message = "Los combos estacionales requieren fechas de vigencia" });
                    }

                    if (actualizarComboDto.FechaInicioVigencia >= actualizarComboDto.FechaFinVigencia)
                    {
                        return BadRequest(new { message = "La fecha de inicio debe ser anterior a la fecha de fin" });
                    }
                }

                // Verificar duplicados (excluyendo el actual)
                var existeCombo = await _context.Combos
                    .AnyAsync(c => c.Id != id && 
                                  c.Nombre.ToLower() == actualizarComboDto.Nombre.ToLower() && 
                                  c.Activo);

                if (existeCombo)
                {
                    return Conflict(new { message = "Ya existe otro combo activo con ese nombre" });
                }

                // Actualizar datos básicos
                _mapper.Map(actualizarComboDto, combo);

                // Actualizar componentes si se especificaron
                if (actualizarComboDto.Componentes != null)
                {
                    // Eliminar componentes existentes
                    _context.ComboComponentes.RemoveRange(combo.Componentes);
                    await _context.SaveChangesAsync();

                    // Agregar nuevos componentes
                    await AgregarComponentesCombo(combo.Id, actualizarComboDto.Componentes);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Combo {Id} actualizado sin redeploy", id);
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar combo {Id}", id);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Activa o desactiva un combo
        /// </summary>
        [HttpPut("{id}/estado")]
        public async Task<IActionResult> CambiarEstadoCombo(int id, CambiarEstadoComboDto cambiarEstadoDto)
        {
            try
            {
                var combo = await _context.Combos.FindAsync(id);
                if (combo == null)
                {
                    return NotFound(new { message = $"Combo con ID {id} no encontrado" });
                }

                combo.Activo = cambiarEstadoDto.Activo;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Combo {Id} {Estado}", id, 
                    cambiarEstadoDto.Activo ? "activado" : "desactivado");
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar estado del combo {Id}", id);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Duplica un combo existente (útil para crear variaciones estacionales)
        /// </summary>
        [HttpPost("{id}/duplicar")]
        public async Task<ActionResult<ComboDto>> DuplicarCombo(int id, DuplicarComboDto duplicarDto)
        {
            try
            {
                var comboOriginal = await _context.Combos
                    .Include(c => c.Componentes)
                        .ThenInclude(cc => cc.Producto)
                    .Include(c => c.Componentes)
                        .ThenInclude(cc => cc.VarianteProducto)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (comboOriginal == null)
                {
                    return NotFound(new { message = $"Combo con ID {id} no encontrado" });
                }

                // Verificar que no exista el nuevo nombre
                var existeCombo = await _context.Combos
                    .AnyAsync(c => c.Nombre.ToLower() == duplicarDto.NuevoNombre.ToLower() && c.Activo);

                if (existeCombo)
                {
                    return Conflict(new { message = "Ya existe un combo activo con ese nombre" });
                }

                // Crear el nuevo combo
                var nuevoCombo = new Combo
                {
                    Nombre = duplicarDto.NuevoNombre,
                    Descripcion = duplicarDto.NuevaDescripcion ?? comboOriginal.Descripcion,
                    Precio = duplicarDto.NuevoPrecio ?? comboOriginal.Precio,
                    TipoCombo = duplicarDto.NuevoTipoCombo,
                    FechaInicioVigencia = duplicarDto.FechaInicioVigencia,
                    FechaFinVigencia = duplicarDto.FechaFinVigencia,
                    Activo = true,
                    EsEditable = true, // Los combos duplicados son editables por defecto
                    FechaCreacion = DateTime.UtcNow
                };

                _context.Combos.Add(nuevoCombo);
                await _context.SaveChangesAsync();

                // Duplicar componentes
                foreach (var componenteOriginal in comboOriginal.Componentes)
                {
                    var nuevoComponente = new ComboComponente
                    {
                        ComboId = nuevoCombo.Id,
                        ProductoId = componenteOriginal.ProductoId,
                        VarianteProductoId = componenteOriginal.VarianteProductoId,
                        Cantidad = componenteOriginal.Cantidad,
                        NombreEspecial = componenteOriginal.NombreEspecial,
                        PrecioEspecial = componenteOriginal.PrecioEspecial
                    };

                    _context.ComboComponentes.Add(nuevoComponente);
                }

                await _context.SaveChangesAsync();

                // Recargar con componentes
                await _context.Entry(nuevoCombo)
                    .Collection(c => c.Componentes)
                    .Query()
                    .Include(cc => cc.Producto)
                    .Include(cc => cc.VarianteProducto)
                    .LoadAsync();

                var comboDto = _mapper.Map<ComboDto>(nuevoCombo);
                
                _logger.LogInformation("Combo {Id} duplicado como {NuevoId}", id, nuevoCombo.Id);
                
                return CreatedAtAction(nameof(GetCombo), new { id = nuevoCombo.Id }, comboDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al duplicar combo {Id}", id);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        // =============================================
        // GESTIÓN DE COMPONENTES
        // =============================================

        /// <summary>
        /// Obtiene los componentes de un combo
        /// </summary>
        [HttpGet("{id}/componentes")]
        public async Task<ActionResult<List<ComboComponenteDto>>> GetComponentesCombo(int id)
        {
            try
            {
                var combo = await _context.Combos.FindAsync(id);
                if (combo == null)
                {
                    return NotFound(new { message = $"Combo con ID {id} no encontrado" });
                }

                var componentes = await _context.ComboComponentes
                    .Where(cc => cc.ComboId == id)
                    .Include(cc => cc.Producto)
                        .ThenInclude(p => p.Categoria)
                    .Include(cc => cc.VarianteProducto)
                    .OrderBy(cc => cc.Id)
                    .ToListAsync();

                var componentesDto = _mapper.Map<List<ComboComponenteDto>>(componentes);
                return Ok(componentesDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener componentes del combo {Id}", id);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Actualiza los componentes de un combo (edición dinámica)
        /// </summary>
        [HttpPut("{id}/componentes")]
        public async Task<IActionResult> ActualizarComponentesCombo(int id, List<ComboComponenteDto> componentesDto)
        {
            try
            {
                var combo = await _context.Combos
                    .Include(c => c.Componentes)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (combo == null)
                {
                    return NotFound(new { message = $"Combo con ID {id} no encontrado" });
                }

                if (!combo.EsEditable)
                {
                    return BadRequest(new { message = "Este combo no es editable" });
                }

                // Eliminar componentes existentes
                _context.ComboComponentes.RemoveRange(combo.Componentes);
                await _context.SaveChangesAsync();

                // Agregar nuevos componentes
                await AgregarComponentesCombo(id, componentesDto);

                _logger.LogInformation("Componentes del combo {Id} actualizados dinámicamente", id);
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar componentes del combo {Id}", id);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        // =============================================
        // PLANTILLAS ESTACIONALES PREDEFINIDAS
        // =============================================

        /// <summary>
        /// Crea combos estacionales basados en plantillas predefinidas
        /// </summary>
        [HttpPost("crear-estacional/{plantilla}")]
        public async Task<ActionResult<ComboDto>> CrearComboEstacionalPlantilla(
            string plantilla, 
            CrearComboEstacionalPlantillaDto crearDto)
        {
            try
            {
                var comboDto = plantilla.ToLower() switch
                {
                    "fiambre" => await CrearComboFiambre(crearDto),
                    "quema-diablo" => await CrearComboQuemaDelDiablo(crearDto),
                    "cuaresma" => await CrearComboCuaresma(crearDto),
                    _ => throw new ArgumentException($"Plantilla '{plantilla}' no reconocida")
                };

                return CreatedAtAction(nameof(GetCombo), new { id = comboDto.Id }, comboDto);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear combo estacional con plantilla {Plantilla}", plantilla);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtiene estadísticas de rendimiento de combos
        /// </summary>
        [HttpGet("estadisticas")]
        public async Task<ActionResult<EstadisticasCombosDto>> GetEstadisticasCombos()
        {
            try
            {
                var combos = await _context.Combos
                    .Include(c => c.DetalleVentas)
                        .ThenInclude(dv => dv.Venta)
                    .ToListAsync();

                var fechaActual = DateTime.UtcNow.Date;

                var estadisticas = new EstadisticasCombosDto
                {
                    TotalCombos = combos.Count,
                    CombosActivos = combos.Count(c => c.Activo),
                    CombosFijos = combos.Count(c => c.TipoCombo == TipoCombo.Fijo),
                    CombosEstacionales = combos.Count(c => c.TipoCombo == TipoCombo.Estacional),
                    CombosVigentes = combos.Count(c => c.Activo && 
                        (c.TipoCombo == TipoCombo.Fijo || 
                         (c.FechaInicioVigencia <= fechaActual && c.FechaFinVigencia >= fechaActual))),
                    TotalVentasCombos = combos.SelectMany(c => c.DetalleVentas).Count(),
                    IngresosTotalesCombos = combos.SelectMany(c => c.DetalleVentas).Sum(dv => dv.Subtotal),
                    ComboMasVendido = combos
                        .Where(c => c.DetalleVentas.Any())
                        .OrderByDescending(c => c.DetalleVentas.Count)
                        .FirstOrDefault()?.Nombre ?? "N/A"
                };

                return Ok(estadisticas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estadísticas de combos");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        // =============================================
        // MÉTODOS PRIVADOS
        // =============================================

        private async Task AgregarComponentesCombo(int comboId, List<ComboComponenteDto> componentesDto)
        {
            foreach (var componenteDto in componentesDto)
            {
                // Validar que el producto/variante existe
                if (componenteDto.ProductoId.HasValue)
                {
                    var producto = await _context.Productos.FindAsync(componenteDto.ProductoId.Value);
                    if (producto == null)
                    {
                        throw new ArgumentException($"Producto con ID {componenteDto.ProductoId} no encontrado");
                    }

                    if (componenteDto.VarianteProductoId.HasValue)
                    {
                        var variante = await _context.VariantesProducto
                            .FirstOrDefaultAsync(vp => vp.Id == componenteDto.VarianteProductoId.Value && 
                                                      vp.ProductoId == componenteDto.ProductoId.Value);
                        if (variante == null)
                        {
                            throw new ArgumentException($"Variante con ID {componenteDto.VarianteProductoId} no encontrada para el producto");
                        }
                    }
                }

                var componente = _mapper.Map<ComboComponente>(componenteDto);
                componente.ComboId = comboId;

                _context.ComboComponentes.Add(componente);
            }

            await _context.SaveChangesAsync();
        }

        private async Task<ComboDto> CrearComboFiambre(CrearComboEstacionalPlantillaDto crearDto)
        {
            var combo = new Combo
            {
                Nombre = crearDto.Nombre ?? "Combo Especial Fiambre",
                Descripcion = "Combo tradicional para la celebración del Día de los Santos con fiambre guatemalteco",
                Precio = crearDto.Precio ?? 185.00m,
                TipoCombo = TipoCombo.Estacional,
                FechaInicioVigencia = crearDto.FechaInicio ?? new DateTime(DateTime.UtcNow.Year, 10, 25),
                FechaFinVigencia = crearDto.FechaFin ?? new DateTime(DateTime.UtcNow.Year, 11, 5),
                Activo = true,
                EsEditable = true,
                FechaCreacion = DateTime.UtcNow
            };

            _context.Combos.Add(combo);
            await _context.SaveChangesAsync();

            // Componentes típicos para fiambre
            var componentes = new List<ComboComponenteDto>
            {
                new() { Cantidad = 2, NombreEspecial = "Tamales para Fiambre", PrecioEspecial = 16.00m },
                new() { Cantidad = 2, NombreEspecial = "Bebida de Cacao Caliente", PrecioEspecial = 24.00m },
                new() { Cantidad = 1, NombreEspecial = "Termo Conmemorativo Santos", PrecioEspecial = 45.00m }
            };

            await AgregarComponentesCombo(combo.Id, componentes);
            return _mapper.Map<ComboDto>(combo);
        }

        private async Task<ComboDto> CrearComboQuemaDelDiablo(CrearComboEstacionalPlantillaDto crearDto)
        {
            var combo = new Combo
            {
                Nombre = crearDto.Nombre ?? "Combo Quema del Diablo",
                Descripcion = "Combo especial para la tradición del 7 de diciembre",
                Precio = crearDto.Precio ?? 165.00m,
                TipoCombo = TipoCombo.Estacional,
                FechaInicioVigencia = crearDto.FechaInicio ?? new DateTime(DateTime.UtcNow.Year, 12, 5),
                FechaFinVigencia = crearDto.FechaFin ?? new DateTime(DateTime.UtcNow.Year, 12, 10),
                Activo = true,
                EsEditable = true,
                FechaCreacion = DateTime.UtcNow
            };

            _context.Combos.Add(combo);
            await _context.SaveChangesAsync();

            var componentes = new List<ComboComponenteDto>
            {
                new() { Cantidad = 6, NombreEspecial = "Tamales Navideños", PrecioEspecial = 12.00m },
                new() { Cantidad = 2, NombreEspecial = "Ponche Navideño", PrecioEspecial = 20.00m },
                new() { Cantidad = 1, NombreEspecial = "Canasta Especial Diciembre", PrecioEspecial = 35.00m }
            };

            await AgregarComponentesCombo(combo.Id, componentes);
            return _mapper.Map<ComboDto>(combo);
        }

        private async Task<ComboDto> CrearComboCuaresma(CrearComboEstacionalPlantillaDto crearDto)
        {
            var combo = new Combo
            {
                Nombre = crearDto.Nombre ?? "Combo Cuaresma Tradicional",
                Descripcion = "Combo especial para Semana Santa con recetas tradicionales de Cuaresma",
                Precio = crearDto.Precio ?? 155.00m,
                TipoCombo = TipoCombo.Estacional,
                FechaInicioVigencia = crearDto.FechaInicio ?? new DateTime(DateTime.UtcNow.Year, 3, 15),
                FechaFinVigencia = crearDto.FechaFin ?? new DateTime(DateTime.UtcNow.Year, 4, 15),
                Activo = true,
                EsEditable = true,
                FechaCreacion = DateTime.UtcNow
            };

            _context.Combos.Add(combo);
            await _context.SaveChangesAsync();

            var componentes = new List<ComboComponenteDto>
            {
                new() { Cantidad = 4, NombreEspecial = "Tamales de Chipilín Cuaresma", PrecioEspecial = 8.50m },
                new() { Cantidad = 2, NombreEspecial = "Bebida de Pinol Tradicional", PrecioEspecial = 15.00m },
                new() { Cantidad = 1, NombreEspecial = "Jarro de Barro Artesanal", PrecioEspecial = 25.00m }
            };

            await AgregarComponentesCombo(combo.Id, componentes);
            return _mapper.Map<ComboDto>(combo);
        }
    }
}