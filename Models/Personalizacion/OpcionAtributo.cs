using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LaCazuelaChapina.API.Models.Ventas;

namespace LaCazuelaChapina.API.Models.Personalizacion
{
    [Table("opciones_atributo")]
    public class OpcionAtributo
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("tipo_atributo_id")]
        public int TipoAtributoId { get; set; }

        [Required(ErrorMessage = "El nombre de la opción es obligatorio")]
        [MaxLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        [Column("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Column("precio_adicional", TypeName = "decimal(10,2)")]
        [Range(0, 999999.99, ErrorMessage = "El precio adicional debe ser mayor o igual a 0")]
        public decimal PrecioAdicional { get; set; } = 0;

        [Column("activa")]
        public bool Activa { get; set; } = true;

        [Column("orden")]
        [Range(0, int.MaxValue, ErrorMessage = "El orden debe ser mayor o igual a 0")]
        public int Orden { get; set; } = 0;

        // Navegación
        public virtual TipoAtributo TipoAtributo { get; set; } = null!;
        public virtual ICollection<PersonalizacionVenta> PersonalizacionesVenta { get; set; } = new List<PersonalizacionVenta>();
    }
}
