using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LaCazuelaChapina.API.Models.Ventas;
using LaCazuelaChapina.API.Models.Combos;

namespace LaCazuelaChapina.API.Models.Productos
{
    [Table("productos")]
    public class Producto
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("categoria_id")]
        public int CategoriaId { get; set; }

        [Required(ErrorMessage = "El nombre del producto es obligatorio")]
        [MaxLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        [Column("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [MaxLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres")]
        [Column("descripcion")]
        public string? Descripcion { get; set; }

        [Column("precio_base", TypeName = "decimal(10,2)")]
        [Range(0.01, 999999.99, ErrorMessage = "El precio debe ser mayor a 0")]
        public decimal PrecioBase { get; set; }

        [Column("activo")]
        public bool Activo { get; set; } = true;

        // Navegación
        public virtual Categoria Categoria { get; set; } = null!;
        public virtual ICollection<VarianteProducto> Variantes { get; set; } = new List<VarianteProducto>();
        public virtual ICollection<DetalleVenta> DetalleVentas { get; set; } = new List<DetalleVenta>();
        public virtual ICollection<ComboComponente> ComboComponentes { get; set; } = new List<ComboComponente>();
    }
}