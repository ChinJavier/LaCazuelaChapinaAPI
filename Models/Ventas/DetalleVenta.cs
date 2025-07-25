using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LaCazuelaChapina.API.Models.Productos;
using LaCazuelaChapina.API.Models.Combos;

namespace LaCazuelaChapina.API.Models.Ventas
{
    [Table("detalle_ventas")]
    public class DetalleVenta
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("venta_id")]
        public int VentaId { get; set; }

        [Column("producto_id")]
        public int? ProductoId { get; set; }

        [Column("variante_producto_id")]
        public int? VarianteProductoId { get; set; }

        [Column("combo_id")]
        public int? ComboId { get; set; }

        [Column("cantidad")]
        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0")]
        public int Cantidad { get; set; }

        [Column("precio_unitario", TypeName = "decimal(10,2)")]
        [Range(0, 999999.99, ErrorMessage = "El precio unitario debe ser mayor o igual a 0")]
        public decimal PrecioUnitario { get; set; }

        [Column("subtotal", TypeName = "decimal(10,2)")]
        [Range(0, 999999.99, ErrorMessage = "El subtotal debe ser mayor o igual a 0")]
        public decimal Subtotal { get; set; }

        [MaxLength(300, ErrorMessage = "Las notas no pueden exceder 300 caracteres")]
        [Column("notas")]
        public string? Notas { get; set; }

        // Navegación
        public virtual Venta Venta { get; set; } = null!;
        public virtual Producto? Producto { get; set; }
        public virtual VarianteProducto? VarianteProducto { get; set; }
        public virtual Combo? Combo { get; set; }
        public virtual ICollection<PersonalizacionVenta> Personalizaciones { get; set; } = new List<PersonalizacionVenta>();
    }
}
