// =============================================
// ARCHIVO: Controllers/PersonalizacionController.cs
// Gestión de Personalización - La Cazuela Chapina
// =============================================

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using LaCazuelaChapina.API.Data;
using LaCazuelaChapina.API.Models.Personalizacion;
using LaCazuelaChapina.API.DTOs.Personalizacion;

namespace LaCazuelaChapina.API.Controllers
{
    /// <summary>
    /// Controlador para la gestión dinámica de personalización de productos
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class PersonalizacionController : ControllerBase
    {
        private readonly CazuelaDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<PersonalizacionController> _logger;

        public PersonalizacionController(
            CazuelaDbContext context,
            IMapper mapper,
            ILogger<PersonalizacionController> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        // =============================================
        // TIPOS DE ATRIBUTO
        // =============================================

        /// <summary>
        /// Obtiene todos los tipos de atributo disponibles
        /// </summary>
        [HttpGet("tipos-atributo")]
        public async Task<ActionResult<List<TipoAtributoDto>>> GetTiposAtributo()
        {
            try
            {
                var tiposAtributo = await _context.TiposAtributo
                    .Include(ta => ta.Categoria)
                    .Include(ta => ta.Opciones.Where(o => o.Activa))
                    .OrderBy(ta => ta.Categoria.Nombre)
                    .ThenBy(ta => ta.Orden)
                    .ToListAsync();

                var tiposAtributoDto = _mapper.Map<List<TipoAtributoDto>>(tiposAtributo);
                return Ok(tiposAtributoDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener tipos de atributo");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtiene los tipos de atributo para una categoría específica
        /// </summary>
        [HttpGet("tipos-atributo/categoria/{categoriaId}")]
        public async Task<ActionResult<List<TipoAtributoDto>>> GetTiposAtributoPorCategoria(int categoriaId)
        {
            try
            {
                var categoria = await _context.Categorias.FindAsync(categoriaId);
                if (categoria == null)
                {
                    return NotFound(new { message = $"Categoría con ID {categoriaId} no encontrada" });
                }

                var tiposAtributo = await _context.TiposAtributo
                    .Where(ta => ta.CategoriaId == categoriaId)
                    .Include(ta => ta.Categoria)
                    .Include(ta => ta.Opciones.Where(o => o.Activa))
                    .OrderBy(ta => ta.Orden)
                    .ToListAsync();

                var tiposAtributoDto = _mapper.Map<List<TipoAtributoDto>>(tiposAtributo);
                return Ok(tiposAtributoDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener tipos de atributo para categoría {CategoriaId}", categoriaId);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtiene un tipo de atributo específico por ID
        /// </summary>
        [HttpGet("tipos-atributo/{id}")]
        public async Task<ActionResult<TipoAtributoDetalleDto>> GetTipoAtributo(int id)
        {
            try
            {
                var tipoAtributo = await _context.TiposAtributo
                    .Include(ta => ta.Categoria)
                    .Include(ta => ta.Opciones)
                    .Include(ta => ta.PersonalizacionesVenta)
                    .FirstOrDefaultAsync(ta => ta.Id == id);

                if (tipoAtributo == null)
                {
                    return NotFound(new { message = $"Tipo de atributo con ID {id} no encontrado" });
                }

                var tipoAtributoDetalle = _mapper.Map<TipoAtributoDetalleDto>(tipoAtributo);
                
                // Agregar estadísticas de uso
                tipoAtributoDetalle.OpcionesActivas = tipoAtributo.Opciones.Count(o => o.Activa);
                tipoAtributoDetalle.VecesUtilizado = tipoAtributo.PersonalizacionesVenta?.Count ?? 0;
                tipoAtributoDetalle.UltimoUso = tipoAtributo.PersonalizacionesVenta?
                    .OrderByDescending(pv => pv.Id)
                    .FirstOrDefault()?.DetalleVenta?.Venta?.FechaVenta;

                return Ok(tipoAtributoDetalle);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener tipo de atributo {Id}", id);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Crea un nuevo tipo de atributo
        /// </summary>
        [HttpPost("tipos-atributo")]
        public async Task<ActionResult<TipoAtributoDto>> CrearTipoAtributo(CrearTipoAtributoDto crearTipoAtributoDto)
        {
            try
            {
                // Verificar que la categoría existe
                var categoria = await _context.Categorias.FindAsync(crearTipoAtributoDto.CategoriaId);
                if (categoria == null)
                {
                    return BadRequest(new { message = "La categoría especificada no existe" });
                }

                // Verificar si ya existe un tipo de atributo con el mismo nombre en la categoría
                var existeTipo = await _context.TiposAtributo
                    .AnyAsync(ta => ta.CategoriaId == crearTipoAtributoDto.CategoriaId && 
                                   ta.Nombre.ToLower() == crearTipoAtributoDto.Nombre.ToLower());

                if (existeTipo)
                {
                    return Conflict(new { message = "Ya existe un tipo de atributo con ese nombre en esta categoría" });
                }

                // Obtener el siguiente orden
                var ultimoOrden = await _context.TiposAtributo
                    .Where(ta => ta.CategoriaId == crearTipoAtributoDto.CategoriaId)
                    .MaxAsync(ta => (int?)ta.Orden) ?? 0;

                var tipoAtributo = _mapper.Map<TipoAtributo>(crearTipoAtributoDto);
                tipoAtributo.Orden = ultimoOrden + 1;

                _context.TiposAtributo.Add(tipoAtributo);
                await _context.SaveChangesAsync();

                // Recargar con incluidos
                await _context.Entry(tipoAtributo)
                    .Reference(ta => ta.Categoria)
                    .LoadAsync();

                var tipoAtributoDto = _mapper.Map<TipoAtributoDto>(tipoAtributo);
                
                _logger.LogInformation("Tipo de atributo {Nombre} creado con ID {Id}", tipoAtributo.Nombre, tipoAtributo.Id);
                
                return CreatedAtAction(nameof(GetTipoAtributo), new { id = tipoAtributo.Id }, tipoAtributoDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear tipo de atributo");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Actualiza un tipo de atributo existente
        /// </summary>
        [HttpPut("tipos-atributo/{id}")]
        public async Task<IActionResult> ActualizarTipoAtributo(int id, ActualizarTipoAtributoDto actualizarTipoAtributoDto)
        {
            try
            {
                var tipoAtributo = await _context.TiposAtributo.FindAsync(id);
                if (tipoAtributo == null)
                {
                    return NotFound(new { message = $"Tipo de atributo con ID {id} no encontrado" });
                }

                // Verificar duplicados (excluyendo el actual)
                var existeTipo = await _context.TiposAtributo
                    .AnyAsync(ta => ta.Id != id && 
                                   ta.CategoriaId == tipoAtributo.CategoriaId && 
                                   ta.Nombre.ToLower() == actualizarTipoAtributoDto.Nombre.ToLower());

                if (existeTipo)
                {
                    return Conflict(new { message = "Ya existe un tipo de atributo con ese nombre en esta categoría" });
                }

                _mapper.Map(actualizarTipoAtributoDto, tipoAtributo);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Tipo de atributo {Id} actualizado", id);
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar tipo de atributo {Id}", id);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Reordena los tipos de atributo de una categoría
        /// </summary>
        [HttpPut("tipos-atributo/categoria/{categoriaId}/reordenar")]
        public async Task<IActionResult> ReordenarTiposAtributo(int categoriaId, ReordenarTiposAtributoDto reordenarDto)
        {
            try
            {
                var categoria = await _context.Categorias.FindAsync(categoriaId);
                if (categoria == null)
                {
                    return NotFound(new { message = $"Categoría con ID {categoriaId} no encontrada" });
                }

                var tiposAtributo = await _context.TiposAtributo
                    .Where(ta => ta.CategoriaId == categoriaId)
                    .ToListAsync();

                // Validar que todos los IDs pertenecen a la categoría
                var idsValidos = tiposAtributo.Select(ta => ta.Id).ToHashSet();
                if (!reordenarDto.IdsOrdenados.All(id => idsValidos.Contains(id)))
                {
                    return BadRequest(new { message = "Algunos IDs no pertenecen a esta categoría" });
                }

                // Aplicar nuevo orden
                for (int i = 0; i < reordenarDto.IdsOrdenados.Count; i++)
                {
                    var tipoAtributo = tiposAtributo.First(ta => ta.Id == reordenarDto.IdsOrdenados[i]);
                    tipoAtributo.Orden = i + 1;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Tipos de atributo reordenados para categoría {CategoriaId}", categoriaId);
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al reordenar tipos de atributo para categoría {CategoriaId}", categoriaId);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        // =============================================
        // OPCIONES DE ATRIBUTO
        // =============================================

        /// <summary>
        /// Obtiene las opciones de un tipo de atributo
        /// </summary>
        [HttpGet("tipos-atributo/{tipoAtributoId}/opciones")]
        public async Task<ActionResult<List<OpcionAtributoDto>>> GetOpcionesAtributo(int tipoAtributoId)
        {
            try
            {
                var tipoAtributo = await _context.TiposAtributo.FindAsync(tipoAtributoId);
                if (tipoAtributo == null)
                {
                    return NotFound(new { message = $"Tipo de atributo con ID {tipoAtributoId} no encontrado" });
                }

                var opciones = await _context.OpcionesAtributo
                    .Where(oa => oa.TipoAtributoId == tipoAtributoId)
                    .Include(oa => oa.TipoAtributo)
                    .OrderBy(oa => oa.Orden)
                    .ToListAsync();

                var opcionesDto = _mapper.Map<List<OpcionAtributoDto>>(opciones);
                return Ok(opcionesDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener opciones del tipo de atributo {TipoAtributoId}", tipoAtributoId);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtiene una opción específica por ID
        /// </summary>
        [HttpGet("opciones/{id}")]
        public async Task<ActionResult<OpcionAtributoDetalleDto>> GetOpcionAtributo(int id)
        {
            try
            {
                var opcion = await _context.OpcionesAtributo
                    .Include(oa => oa.TipoAtributo)
                        .ThenInclude(ta => ta.Categoria)
                    .Include(oa => oa.PersonalizacionesVenta)
                    .FirstOrDefaultAsync(oa => oa.Id == id);

                if (opcion == null)
                {
                    return NotFound(new { message = $"Opción de atributo con ID {id} no encontrada" });
                }

                var opcionDetalle = _mapper.Map<OpcionAtributoDetalleDto>(opcion);
                
                // Agregar estadísticas de uso
                opcionDetalle.VecesUtilizada = opcion.PersonalizacionesVenta?.Count ?? 0;
                opcionDetalle.UltimoUso = opcion.PersonalizacionesVenta?
                    .OrderByDescending(pv => pv.Id)
                    .FirstOrDefault()?.DetalleVenta?.Venta?.FechaVenta;

                return Ok(opcionDetalle);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener opción de atributo {Id}", id);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Crea una nueva opción de atributo
        /// </summary>
        [HttpPost("tipos-atributo/{tipoAtributoId}/opciones")]
        public async Task<ActionResult<OpcionAtributoDto>> CrearOpcionAtributo(
            int tipoAtributoId, 
            CrearOpcionAtributoDto crearOpcionDto)
        {
            try
            {
                var tipoAtributo = await _context.TiposAtributo.FindAsync(tipoAtributoId);
                if (tipoAtributo == null)
                {
                    return NotFound(new { message = $"Tipo de atributo con ID {tipoAtributoId} no encontrado" });
                }

                // Verificar duplicados
                var existeOpcion = await _context.OpcionesAtributo
                    .AnyAsync(oa => oa.TipoAtributoId == tipoAtributoId && 
                                   oa.Nombre.ToLower() == crearOpcionDto.Nombre.ToLower());

                if (existeOpcion)
                {
                    return Conflict(new { message = "Ya existe una opción con ese nombre para este tipo de atributo" });
                }

                // Obtener el siguiente orden
                var ultimoOrden = await _context.OpcionesAtributo
                    .Where(oa => oa.TipoAtributoId == tipoAtributoId)
                    .MaxAsync(oa => (int?)oa.Orden) ?? 0;

                var opcion = _mapper.Map<OpcionAtributo>(crearOpcionDto);
                opcion.TipoAtributoId = tipoAtributoId;
                opcion.Orden = ultimoOrden + 1;
                opcion.Activa = true;

                _context.OpcionesAtributo.Add(opcion);
                await _context.SaveChangesAsync();

                // Recargar con incluidos
                await _context.Entry(opcion)
                    .Reference(oa => oa.TipoAtributo)
                    .LoadAsync();

                var opcionDto = _mapper.Map<OpcionAtributoDto>(opcion);
                
                _logger.LogInformation("Opción {Nombre} creada para tipo de atributo {TipoAtributoId}", 
                    opcion.Nombre, tipoAtributoId);
                
                return CreatedAtAction(nameof(GetOpcionAtributo), new { id = opcion.Id }, opcionDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear opción de atributo");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Actualiza una opción de atributo existente
        /// </summary>
        [HttpPut("opciones/{id}")]
        public async Task<IActionResult> ActualizarOpcionAtributo(int id, ActualizarOpcionAtributoDto actualizarOpcionDto)
        {
            try
            {
                var opcion = await _context.OpcionesAtributo.FindAsync(id);
                if (opcion == null)
                {
                    return NotFound(new { message = $"Opción de atributo con ID {id} no encontrada" });
                }

                // Verificar duplicados (excluyendo la actual)
                var existeOpcion = await _context.OpcionesAtributo
                    .AnyAsync(oa => oa.Id != id && 
                                   oa.TipoAtributoId == opcion.TipoAtributoId && 
                                   oa.Nombre.ToLower() == actualizarOpcionDto.Nombre.ToLower());

                if (existeOpcion)
                {
                    return Conflict(new { message = "Ya existe una opción con ese nombre para este tipo de atributo" });
                }

                _mapper.Map(actualizarOpcionDto, opcion);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Opción de atributo {Id} actualizada", id);
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar opción de atributo {Id}", id);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Activa o desactiva una opción de atributo
        /// </summary>
        [HttpPut("opciones/{id}/estado")]
        public async Task<IActionResult> CambiarEstadoOpcion(int id, CambiarEstadoOpcionDto cambiarEstadoDto)
        {
            try
            {
                var opcion = await _context.OpcionesAtributo
                    .Include(oa => oa.TipoAtributo)
                    .FirstOrDefaultAsync(oa => oa.Id == id);
                
                if (opcion == null)
                {
                    return NotFound(new { message = $"Opción de atributo con ID {id} no encontrada" });
                }

                // Si se está desactivando, verificar que no sea la única opción activa de un atributo obligatorio
                if (!cambiarEstadoDto.Activa && opcion.TipoAtributo.EsObligatorio)
                {
                    var opcionesActivas = await _context.OpcionesAtributo
                        .CountAsync(oa => oa.TipoAtributoId == opcion.TipoAtributoId && oa.Activa && oa.Id != id);

                    if (opcionesActivas == 0)
                    {
                        return BadRequest(new { message = "No se puede desactivar la única opción activa de un atributo obligatorio" });
                    }
                }

                opcion.Activa = cambiarEstadoDto.Activa;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Opción de atributo {Id} {Estado}", id, 
                    cambiarEstadoDto.Activa ? "activada" : "desactivada");
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar estado de opción {Id}", id);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Reordena las opciones de un tipo de atributo
        /// </summary>
        [HttpPut("tipos-atributo/{tipoAtributoId}/opciones/reordenar")]
        public async Task<IActionResult> ReordenarOpciones(int tipoAtributoId, ReordenarOpcionesDto reordenarDto)
        {
            try
            {
                var tipoAtributo = await _context.TiposAtributo.FindAsync(tipoAtributoId);
                if (tipoAtributo == null)
                {
                    return NotFound(new { message = $"Tipo de atributo con ID {tipoAtributoId} no encontrado" });
                }

                var opciones = await _context.OpcionesAtributo
                    .Where(oa => oa.TipoAtributoId == tipoAtributoId)
                    .ToListAsync();

                // Validar que todos los IDs pertenecen al tipo de atributo
                var idsValidos = opciones.Select(oa => oa.Id).ToHashSet();
                if (!reordenarDto.IdsOrdenados.All(id => idsValidos.Contains(id)))
                {
                    return BadRequest(new { message = "Algunos IDs no pertenecen a este tipo de atributo" });
                }

                // Aplicar nuevo orden
                for (int i = 0; i < reordenarDto.IdsOrdenados.Count; i++)
                {
                    var opcion = opciones.First(oa => oa.Id == reordenarDto.IdsOrdenados[i]);
                    opcion.Orden = i + 1;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Opciones reordenadas para tipo de atributo {TipoAtributoId}", tipoAtributoId);
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al reordenar opciones para tipo de atributo {TipoAtributoId}", tipoAtributoId);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        // =============================================
        // ESTADÍSTICAS Y REPORTES
        // =============================================

        /// <summary>
        /// Obtiene estadísticas de uso de personalización
        /// </summary>
        [HttpGet("estadisticas")]
        public async Task<ActionResult<EstadisticasPersonalizacionDto>> GetEstadisticasPersonalizacion()
        {
            try
            {
                var tiposAtributo = await _context.TiposAtributo
                    .Include(ta => ta.Categoria)
                    .Include(ta => ta.Opciones)
                    .Include(ta => ta.PersonalizacionesVenta)
                    .ToListAsync();

                var estadisticas = new EstadisticasPersonalizacionDto
                {
                    TotalTiposAtributo = tiposAtributo.Count,
                    TotalOpciones = tiposAtributo.SelectMany(ta => ta.Opciones).Count(),
                    OpcionesActivas = tiposAtributo.SelectMany(ta => ta.Opciones).Count(o => o.Activa),
                    TotalPersonalizacionesVendidas = tiposAtributo.SelectMany(ta => ta.PersonalizacionesVenta).Count(),
                    EstadisticasPorCategoria = tiposAtributo
                        .GroupBy(ta => ta.Categoria)
                        .Select(g => new EstadisticasCategoriaDto
                        {
                            CategoriaId = g.Key.Id,
                            CategoriaNombre = g.Key.Nombre,
                            TiposAtributo = g.Count(),
                            TotalOpciones = g.SelectMany(ta => ta.Opciones).Count(),
                            OpcionesActivas = g.SelectMany(ta => ta.Opciones).Count(o => o.Activa),
                            PersonalizacionesVendidas = g.SelectMany(ta => ta.PersonalizacionesVenta).Count()
                        })
                        .ToList(),
                    OpcionesMasUsadas = tiposAtributo
                        .SelectMany(ta => ta.Opciones)
                        .Select(o => new OpcionMasUsadaDto
                        {
                            OpcionId = o.Id,
                            OpcionNombre = o.Nombre,
                            TipoAtributoNombre = o.TipoAtributo.Nombre,
                            CategoriaNombre = o.TipoAtributo.Categoria.Nombre,
                            VecesUsada = o.PersonalizacionesVenta?.Count ?? 0,
                            PrecioAdicional = o.PrecioAdicional
                        })
                        .Where(o => o.VecesUsada > 0)
                        .OrderByDescending(o => o.VecesUsada)
                        .Take(10)
                        .ToList()
                };

                return Ok(estadisticas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estadísticas de personalización");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Duplica un tipo de atributo con todas sus opciones
        /// </summary>
        [HttpPost("tipos-atributo/{id}/duplicar")]
        public async Task<ActionResult<TipoAtributoDto>> DuplicarTipoAtributo(int id, DuplicarTipoAtributoDto duplicarDto)
        {
            try
            {
                var tipoAtributoOriginal = await _context.TiposAtributo
                    .Include(ta => ta.Opciones)
                    .FirstOrDefaultAsync(ta => ta.Id == id);

                if (tipoAtributoOriginal == null)
                {
                    return NotFound(new { message = $"Tipo de atributo con ID {id} no encontrado" });
                }

                // Verificar que no exista el nuevo nombre
                var existeTipo = await _context.TiposAtributo
                    .AnyAsync(ta => ta.CategoriaId == duplicarDto.NuevaCategoriaId && 
                                   ta.Nombre.ToLower() == duplicarDto.NuevoNombre.ToLower());

                if (existeTipo)
                {
                    return Conflict(new { message = "Ya existe un tipo de atributo con ese nombre en la categoría destino" });
                }

                // Obtener el siguiente orden en la categoría destino
                var ultimoOrden = await _context.TiposAtributo
                    .Where(ta => ta.CategoriaId == duplicarDto.NuevaCategoriaId)
                    .MaxAsync(ta => (int?)ta.Orden) ?? 0;

                // Crear el nuevo tipo de atributo
                var nuevoTipoAtributo = new TipoAtributo
                {
                    Nombre = duplicarDto.NuevoNombre,
                    CategoriaId = duplicarDto.NuevaCategoriaId,
                    EsObligatorio = tipoAtributoOriginal.EsObligatorio,
                    PermiteMultiple = tipoAtributoOriginal.PermiteMultiple,
                    Orden = ultimoOrden + 1
                };

                _context.TiposAtributo.Add(nuevoTipoAtributo);
                await _context.SaveChangesAsync();

                // Duplicar las opciones
                foreach (var opcionOriginal in tipoAtributoOriginal.Opciones)
                {
                    var nuevaOpcion = new OpcionAtributo
                    {
                        TipoAtributoId = nuevoTipoAtributo.Id,
                        Nombre = opcionOriginal.Nombre,
                        PrecioAdicional = opcionOriginal.PrecioAdicional,
                        Activa = opcionOriginal.Activa,
                        Orden = opcionOriginal.Orden
                    };

                    _context.OpcionesAtributo.Add(nuevaOpcion);
                }

                await _context.SaveChangesAsync();

                // Recargar con incluidos
                await _context.Entry(nuevoTipoAtributo)
                    .Reference(ta => ta.Categoria)
                    .LoadAsync();
                await _context.Entry(nuevoTipoAtributo)
                    .Collection(ta => ta.Opciones)
                    .LoadAsync();

                var tipoAtributoDto = _mapper.Map<TipoAtributoDto>(nuevoTipoAtributo);
                
                _logger.LogInformation("Tipo de atributo {Id} duplicado como {NuevoId}", id, nuevoTipoAtributo.Id);
                
                return CreatedAtAction(nameof(GetTipoAtributo), new { id = nuevoTipoAtributo.Id }, tipoAtributoDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al duplicar tipo de atributo {Id}", id);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }
    }
}