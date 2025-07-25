using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaCazuelaChapina.API.Models.Inventario
{
    [Table("categorias_materias_primas")]
    public class CategoriaMateriaPrima
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre de la categoría es obligatorio")]
        [MaxLength(50, ErrorMessage = "El nombre no puede exceder 50 caracteres")]
        [Column("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [MaxLength(200, ErrorMessage = "La descripción no puede exceder 200 caracteres")]
        [Column("descripcion")]
        public string? Descripcion { get; set; }

        // Navegación
        public virtual ICollection<MateriaPrima> MateriasPrimas { get; set; } = new List<MateriaPrima>();
    }
}
