// =============================================
// ARCHIVO: Data/CazuelaDbContext.cs
// DbContext Principal - La Cazuela Chapina
// =============================================

using Microsoft.EntityFrameworkCore;
using LaCazuelaChapina.API.Models.Enums;
using LaCazuelaChapina.API.Models.Sucursales;
using LaCazuelaChapina.API.Models.Productos;
using LaCazuelaChapina.API.Models.Personalizacion;
using LaCazuelaChapina.API.Models.Combos;
using LaCazuelaChapina.API.Models.Inventario;
using LaCazuelaChapina.API.Models.Ventas;
using LaCazuelaChapina.API.Models.Notificaciones;

namespace LaCazuelaChapina.API.Data
{
    /// <summary>
    /// Contexto principal de Entity Framework para La Cazuela Chapina
    /// </summary>
    public class CazuelaDbContext : DbContext
    {
        public CazuelaDbContext(DbContextOptions<CazuelaDbContext> options) : base(options)
        {
        }

        // =============================================
        // DbSets - Tablas de la Base de Datos
        // =============================================

        // Gestión de Sucursales
        public DbSet<Sucursal> Sucursales { get; set; }

        // Catálogo de Productos
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Producto> Productos { get; set; }
        public DbSet<VarianteProducto> VariantesProducto { get; set; }

        // Sistema de Personalización
        public DbSet<TipoAtributo> TiposAtributo { get; set; }
        public DbSet<OpcionAtributo> OpcionesAtributo { get; set; }

        // Sistema de Combos
        public DbSet<Combo> Combos { get; set; }
        public DbSet<ComboComponente> ComboComponentes { get; set; }

        // Gestión de Inventario
        public DbSet<CategoriaMateriaPrima> CategoriasMateriasPrimas { get; set; }
        public DbSet<MateriaPrima> MateriasPrimas { get; set; }
        public DbSet<StockSucursal> StockSucursal { get; set; }
        public DbSet<MovimientoInventario> MovimientosInventario { get; set; }

        // Sistema de Ventas
        public DbSet<Venta> Ventas { get; set; }
        public DbSet<DetalleVenta> DetalleVentas { get; set; }
        public DbSet<PersonalizacionVenta> PersonalizacionesVenta { get; set; }

        // Sistema de Notificaciones
        public DbSet<Notificacion> Notificaciones { get; set; }

        // =============================================
        // Configuración del Modelo
        // =============================================

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configurar ENUMs de PostgreSQL
            ConfigureEnums(modelBuilder);

            // Configurar relaciones y constraints
            ConfigureRelationships(modelBuilder);

            // Configurar índices
            ConfigureIndexes(modelBuilder);

            // Configurar constraints adicionales
            ConfigureConstraints(modelBuilder);
        }

        private void ConfigureEnums(ModelBuilder modelBuilder)
        {
            // Configurar ENUMs de PostgreSQL
            modelBuilder.HasPostgresEnum<TipoCombo>("tipo_combo");
            modelBuilder.HasPostgresEnum<TipoMovimiento>("tipo_movimiento");
            modelBuilder.HasPostgresEnum<TipoPago>("tipo_pago");
            modelBuilder.HasPostgresEnum<EstadoVenta>("estado_venta");
            modelBuilder.HasPostgresEnum<TipoNotificacion>("tipo_notificacion");

                // Configurar conversiones explícitas para ENUMs
            modelBuilder.Entity<Venta>()
                .Property(e => e.EstadoVenta)
                .HasConversion<string>();

            modelBuilder.Entity<Venta>()
                .Property(e => e.TipoPago)
                .HasConversion<string>();

            modelBuilder.Entity<MovimientoInventario>()
                .Property(e => e.TipoMovimiento)
                .HasConversion<string>();

            modelBuilder.Entity<Combo>()
                .Property(e => e.TipoCombo)
                .HasConversion<string>();
                }

        private void ConfigureRelationships(ModelBuilder modelBuilder)
        {
            // =============================================
            // Relaciones Categorías -> Productos
            // =============================================
            modelBuilder.Entity<Producto>()
                .HasOne(p => p.Categoria)
                .WithMany(c => c.Productos)
                .HasForeignKey(p => p.CategoriaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<VarianteProducto>()
                .HasOne(vp => vp.Producto)
                .WithMany(p => p.Variantes)
                .HasForeignKey(vp => vp.ProductoId)
                .OnDelete(DeleteBehavior.Cascade);

            // =============================================
            // Relaciones Sistema de Personalización
            // =============================================
            modelBuilder.Entity<TipoAtributo>()
                .HasOne(ta => ta.Categoria)
                .WithMany(c => c.TiposAtributo)
                .HasForeignKey(ta => ta.CategoriaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OpcionAtributo>()
                .HasOne(oa => oa.TipoAtributo)
                .WithMany(ta => ta.Opciones)
                .HasForeignKey(oa => oa.TipoAtributoId)
                .OnDelete(DeleteBehavior.Cascade);

            // =============================================
            // Relaciones Sistema de Combos
            // =============================================
            modelBuilder.Entity<ComboComponente>()
                .HasOne(cc => cc.Combo)
                .WithMany(c => c.Componentes)
                .HasForeignKey(cc => cc.ComboId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ComboComponente>()
                .HasOne(cc => cc.Producto)
                .WithMany(p => p.ComboComponentes)
                .HasForeignKey(cc => cc.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ComboComponente>()
                .HasOne(cc => cc.VarianteProducto)
                .WithMany(vp => vp.ComboComponentes)
                .HasForeignKey(cc => cc.VarianteProductoId)
                .OnDelete(DeleteBehavior.Restrict);

            // =============================================
            // Relaciones Inventario
            // =============================================
            modelBuilder.Entity<MateriaPrima>()
                .HasOne(mp => mp.Categoria)
                .WithMany(cmp => cmp.MateriasPrimas)
                .HasForeignKey(mp => mp.CategoriaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockSucursal>()
                .HasOne(ss => ss.Sucursal)
                .WithMany(s => s.Stocks)
                .HasForeignKey(ss => ss.SucursalId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StockSucursal>()
                .HasOne(ss => ss.MateriaPrima)
                .WithMany(mp => mp.Stocks)
                .HasForeignKey(ss => ss.MateriaPrimaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MovimientoInventario>()
                .HasOne(mi => mi.Sucursal)
                .WithMany(s => s.MovimientosInventario)
                .HasForeignKey(mi => mi.SucursalId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MovimientoInventario>()
                .HasOne(mi => mi.MateriaPrima)
                .WithMany(mp => mp.Movimientos)
                .HasForeignKey(mi => mi.MateriaPrimaId)
                .OnDelete(DeleteBehavior.Restrict);

            // =============================================
            // Relaciones Sistema de Ventas
            // =============================================
            modelBuilder.Entity<Venta>()
                .HasOne(v => v.Sucursal)
                .WithMany(s => s.Ventas)
                .HasForeignKey(v => v.SucursalId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DetalleVenta>()
                .HasOne(dv => dv.Venta)
                .WithMany(v => v.Detalles)
                .HasForeignKey(dv => dv.VentaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DetalleVenta>()
                .HasOne(dv => dv.Producto)
                .WithMany(p => p.DetalleVentas)
                .HasForeignKey(dv => dv.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DetalleVenta>()
                .HasOne(dv => dv.VarianteProducto)
                .WithMany(vp => vp.DetalleVentas)
                .HasForeignKey(dv => dv.VarianteProductoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DetalleVenta>()
                .HasOne(dv => dv.Combo)
                .WithMany(c => c.DetalleVentas)
                .HasForeignKey(dv => dv.ComboId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PersonalizacionVenta>()
                .HasOne(pv => pv.DetalleVenta)
                .WithMany(dv => dv.Personalizaciones)
                .HasForeignKey(pv => pv.DetalleVentaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PersonalizacionVenta>()
                .HasOne(pv => pv.TipoAtributo)
                .WithMany(ta => ta.PersonalizacionesVenta)
                .HasForeignKey(pv => pv.TipoAtributoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PersonalizacionVenta>()
                .HasOne(pv => pv.OpcionAtributo)
                .WithMany(oa => oa.PersonalizacionesVenta)
                .HasForeignKey(pv => pv.OpcionAtributoId)
                .OnDelete(DeleteBehavior.Restrict);

            // =============================================
            // Relaciones Notificaciones
            // =============================================
            modelBuilder.Entity<Notificacion>()
                .HasOne(n => n.Sucursal)
                .WithMany(s => s.Notificaciones)
                .HasForeignKey(n => n.SucursalId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        private void ConfigureIndexes(ModelBuilder modelBuilder)
        {
            // Índices para optimización de consultas frecuentes
            
            // Ventas por sucursal y fecha
            modelBuilder.Entity<Venta>()
                .HasIndex(v => new { v.SucursalId, v.FechaVenta })
                .HasDatabaseName("idx_ventas_sucursal_fecha");

            // Stock por sucursal y materia prima (único)
            modelBuilder.Entity<StockSucursal>()
                .HasIndex(ss => new { ss.SucursalId, ss.MateriaPrimaId })
                .IsUnique()
                .HasDatabaseName("idx_stock_sucursal_lookup");

            // Movimientos de inventario por sucursal y fecha
            modelBuilder.Entity<MovimientoInventario>()
                .HasIndex(mi => new { mi.SucursalId, mi.FechaMovimiento })
                .HasDatabaseName("idx_movimientos_inventario_sucursal_fecha");

            // Personalización por detalle de venta
            modelBuilder.Entity<PersonalizacionVenta>()
                .HasIndex(pv => pv.DetalleVentaId)
                .HasDatabaseName("idx_personalizacion_venta_detalle");

            // Combos estacionales por vigencia
            modelBuilder.Entity<Combo>()
                .HasIndex(c => new { c.FechaInicioVigencia, c.FechaFinVigencia })
                .HasDatabaseName("idx_combos_vigencia")
                .HasFilter("tipo_combo = 'Estacional'");

            // Número de venta único por sucursal
            modelBuilder.Entity<Venta>()
                .HasIndex(v => new { v.SucursalId, v.NumeroVenta })
                .IsUnique()
                .HasDatabaseName("idx_venta_numero_unico");

            // Notificaciones por sucursal y estado
            modelBuilder.Entity<Notificacion>()
                .HasIndex(n => new { n.SucursalId, n.Enviada })
                .HasDatabaseName("idx_notificaciones_sucursal_estado");
        }

        private void ConfigureConstraints(ModelBuilder modelBuilder)
        {
            // Constraint para DetalleVenta (Producto+Variante O Combo, no ambos)
            modelBuilder.Entity<DetalleVenta>()
                .HasCheckConstraint("CK_detalle_venta_tipo", 
                    "(producto_id IS NOT NULL AND variante_producto_id IS NOT NULL AND combo_id IS NULL) OR " +
                    "(producto_id IS NULL AND variante_producto_id IS NULL AND combo_id IS NOT NULL)");

            // Constraint para precios positivos
            modelBuilder.Entity<Producto>()
                .HasCheckConstraint("CK_producto_precio_positivo", "precio_base > 0");

            modelBuilder.Entity<Combo>()
                .HasCheckConstraint("CK_combo_precio_positivo", "precio > 0");

            modelBuilder.Entity<VarianteProducto>()
                .HasCheckConstraint("CK_variante_multiplicador_positivo", "multiplicador > 0");

            // Constraint para stock no negativo
            modelBuilder.Entity<StockSucursal>()
                .HasCheckConstraint("CK_stock_no_negativo", "cantidad_actual >= 0");

            modelBuilder.Entity<MateriaPrima>()
                .HasCheckConstraint("CK_materia_prima_stocks", 
                    "stock_minimo >= 0 AND stock_maximo >= stock_minimo AND costo_promedio >= 0");

            // Constraint para combos estacionales
            modelBuilder.Entity<Combo>()
                .HasCheckConstraint("CK_combo_fechas_vigencia",
                    "tipo_combo = 'Fijo' OR (fecha_inicio_vigencia IS NOT NULL AND fecha_fin_vigencia IS NOT NULL AND fecha_inicio_vigencia <= fecha_fin_vigencia)");
        }

        // =============================================
        // Métodos de Utilidad Override
        // =============================================

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // Actualizar timestamps automáticamente
            UpdateTimestamps();
            
            return await base.SaveChangesAsync(cancellationToken);
        }

        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        private void UpdateTimestamps()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                // Actualizar FechaActualizacion en Sucursales
                if (entry.Entity is Sucursal sucursal && entry.State == EntityState.Modified)
                {
                    sucursal.FechaActualizacion = DateTime.UtcNow;
                }

                // Actualizar FechaUltimaActualizacion en StockSucursal
                if (entry.Entity is StockSucursal stock && entry.State == EntityState.Modified)
                {
                    stock.FechaUltimaActualizacion = DateTime.UtcNow;
                }
            }
        }
    }
}
