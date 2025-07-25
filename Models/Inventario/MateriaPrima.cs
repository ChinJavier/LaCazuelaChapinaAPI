using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaCazuelaChapina.API.Models.Inventario
{
    [Table("materias_primas")]
    public class MateriaPrima
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("categoria_id")]
        public int CategoriaId { get; set; }

        [Required(ErrorMessage = "El nombre de la materia prima es obligatorio")]
        [MaxLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        [Column("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "La unidad de medida es obligatoria")]
        [MaxLength(20, ErrorMessage = "La unidad de medida no puede exceder 20 caracteres")]
        [Column("unidad_medida")]
        public string UnidadMedida { get; set; } = string.Empty;

        [Column("stock_minimo", TypeName = "decimal(10,3)")]
        [Range(0, 999999.999, ErrorMessage = "El stock mínimo debe ser mayor o igual a 0")]
        public decimal StockMinimo { get; set; }

        [Column("stock_maximo", TypeName = "decimal(10,3)")]
        [Range(0, 999999.999, ErrorMessage = "El stock máximo debe ser mayor o igual a 0")]
        public decimal StockMaximo { get; set; }

        [Column("costo_promedio", TypeName = "decimal(10,4)")]
        [Range(0, 999999.9999, ErrorMessage = "El costo promedio debe ser mayor o igual a 0")]
        public decimal CostoPromedio { get; set; }

        [Column("activa")]
        public bool Activa { get; set; } = true;

        // Navegación
        public virtual CategoriaMateriaPrima Categoria { get; set; } = null!;
        public virtual ICollection<StockSucursal> Stocks { get; set; } = new List<StockSucursal>();
        public virtual ICollection<MovimientoInventario> Movimientos { get; set; } = new List<MovimientoInventario>();
    }
}