using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LaCazuelaChapina.API.Models.Enums;
using LaCazuelaChapina.API.Models.Sucursales;

namespace LaCazuelaChapina.API.Models.Ventas
{
    [Table("ventas")]
    public class Venta
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("sucursal_id")]
        public int SucursalId { get; set; }

        [Required(ErrorMessage = "El número de venta es obligatorio")]
        [MaxLength(20, ErrorMessage = "El número de venta no puede exceder 20 caracteres")]
        [Column("numero_venta")]
        public string NumeroVenta { get; set; } = string.Empty;

        [Column("fecha_venta")]
        public DateTime FechaVenta { get; set; } = DateTime.UtcNow;

        [Column("subtotal", TypeName = "decimal(10,2)")]
        [Range(0, 999999.99, ErrorMessage = "El subtotal debe ser mayor o igual a 0")]
        public decimal Subtotal { get; set; }

        [Column("descuento", TypeName = "decimal(10,2)")]
        [Range(0, 999999.99, ErrorMessage = "El descuento debe ser mayor o igual a 0")]
        public decimal Descuento { get; set; } = 0;

        [Column("total", TypeName = "decimal(10,2)")]
        [Range(0, 999999.99, ErrorMessage = "El total debe ser mayor o igual a 0")]
        public decimal Total { get; set; }

        [Column("tipo_pago")]
        public TipoPago? TipoPago { get; set; }

        [Column("estado_venta")]
        public EstadoVenta EstadoVenta { get; set; } = EstadoVenta.Completada;

        [Column("es_venta_offline")]
        public bool EsVentaOffline { get; set; } = false;

        [Column("fecha_sincronizacion")]
        public DateTime? FechaSincronizacion { get; set; }

        [MaxLength(100, ErrorMessage = "El nombre del cliente no puede exceder 100 caracteres")]
        [Column("cliente_nombre")]
        public string? ClienteNombre { get; set; }

        [MaxLength(15, ErrorMessage = "El teléfono no puede exceder 15 caracteres")]
        [Phone(ErrorMessage = "Formato de teléfono inválido")]
        [Column("cliente_telefono")]
        public string? ClienteTelefono { get; set; }

        [Column("usuario_id")]
        public int? UsuarioId { get; set; }

        // Navegación
        public virtual Sucursal Sucursal { get; set; } = null!;
        public virtual ICollection<DetalleVenta> Detalles { get; set; } = new List<DetalleVenta>();
    }
}