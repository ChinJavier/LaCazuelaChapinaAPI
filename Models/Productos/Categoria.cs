using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LaCazuelaChapina.API.Models.Personalizacion;

namespace LaCazuelaChapina.API.Models.Productos
{
    [Table("categorias")]
    public class Categoria
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

        [Column("activa")]
        public bool Activa { get; set; } = true;

        // Navegación
        public virtual ICollection<Producto> Productos { get; set; } = new List<Producto>();
        public virtual ICollection<TipoAtributo> TiposAtributo { get; set; } = new List<TipoAtributo>();
    }
}