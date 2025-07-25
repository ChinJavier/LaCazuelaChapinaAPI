using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LaCazuelaChapina.API.Models.Ventas;
using LaCazuelaChapina.API.Models.Combos;

namespace LaCazuelaChapina.API.Models.Productos
{
    [Table("variantes_producto")]
    public class VarianteProducto
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("producto_id")]
        public int ProductoId { get; set; }

        [Required(ErrorMessage = "El nombre de la variante es obligatorio")]
        [MaxLength(50, ErrorMessage = "El nombre no puede exceder 50 caracteres")]
        [Column("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Column("multiplicador", TypeName = "decimal(5,2)")]
        [Range(0.01, 999.99, ErrorMessage = "El multiplicador debe ser mayor a 0")]
        public decimal Multiplicador { get; set; }

        [Column("cantidad_unidades")]
        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0")]
        public int CantidadUnidades { get; set; } = 1;

        [Column("volumen_ml")]
        [Range(1, int.MaxValue, ErrorMessage = "El volumen debe ser mayor a 0")]
        public int? VolumenMl { get; set; }

        [Column("activa")]
        public bool Activa { get; set; } = true;

        // Navegación
        public virtual Producto Producto { get; set; } = null!;
        public virtual ICollection<DetalleVenta> DetalleVentas { get; set; } = new List<DetalleVenta>();
        public virtual ICollection<ComboComponente> ComboComponentes { get; set; } = new List<ComboComponente>();
    }
}