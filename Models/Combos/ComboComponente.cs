using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LaCazuelaChapina.API.Models.Productos;

namespace LaCazuelaChapina.API.Models.Combos
{
    [Table("combo_componentes")]
    public class ComboComponente
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("combo_id")]
        public int ComboId { get; set; }

        [Column("producto_id")]
        public int? ProductoId { get; set; }

        [Column("variante_producto_id")]
        public int? VarianteProductoId { get; set; }

        [Column("cantidad")]
        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0")]
        public int Cantidad { get; set; }

        [MaxLength(100, ErrorMessage = "El nombre especial no puede exceder 100 caracteres")]
        [Column("nombre_especial")]
        public string? NombreEspecial { get; set; }

        [Column("precio_especial", TypeName = "decimal(10,2)")]
        [Range(0, 999999.99, ErrorMessage = "El precio especial debe ser mayor o igual a 0")]
        public decimal? PrecioEspecial { get; set; }

        // Navegación
        public virtual Combo Combo { get; set; } = null!;
        public virtual Producto? Producto { get; set; }
        public virtual VarianteProducto? VarianteProducto { get; set; }
    }
}
