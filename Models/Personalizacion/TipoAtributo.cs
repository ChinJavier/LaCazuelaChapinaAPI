using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LaCazuelaChapina.API.Models.Productos;
using LaCazuelaChapina.API.Models.Ventas;

namespace LaCazuelaChapina.API.Models.Personalizacion
{
    [Table("tipos_atributo")]
    public class TipoAtributo
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre del tipo de atributo es obligatorio")]
        [MaxLength(50, ErrorMessage = "El nombre no puede exceder 50 caracteres")]
        [Column("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Column("categoria_id")]
        public int CategoriaId { get; set; }

        [Column("es_obligatorio")]
        public bool EsObligatorio { get; set; } = true;

        [Column("permite_multiple")]
        public bool PermiteMultiple { get; set; } = false;

        [Column("orden")]
        [Range(0, int.MaxValue, ErrorMessage = "El orden debe ser mayor o igual a 0")]
        public int Orden { get; set; } = 0;

        // Navegación
        public virtual Categoria Categoria { get; set; } = null!;
        public virtual ICollection<OpcionAtributo> Opciones { get; set; } = new List<OpcionAtributo>();
        public virtual ICollection<PersonalizacionVenta> PersonalizacionesVenta { get; set; } = new List<PersonalizacionVenta>();
    }
}