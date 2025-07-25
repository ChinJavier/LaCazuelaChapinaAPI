using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LaCazuelaChapina.API.Models.Personalizacion;

namespace LaCazuelaChapina.API.Models.Ventas
{
    [Table("personalizacion_venta")]
    public class PersonalizacionVenta
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("detalle_venta_id")]
        public int DetalleVentaId { get; set; }

        [Column("tipo_atributo_id")]
        public int TipoAtributoId { get; set; }

        [Column("opcion_atributo_id")]
        public int OpcionAtributoId { get; set; }

        [Column("precio_adicional", TypeName = "decimal(10,2)")]
        [Range(0, 999999.99, ErrorMessage = "El precio adicional debe ser mayor o igual a 0")]
        public decimal PrecioAdicional { get; set; } = 0;

        // Navegación
        public virtual DetalleVenta DetalleVenta { get; set; } = null!;
        public virtual TipoAtributo TipoAtributo { get; set; } = null!;
        public virtual OpcionAtributo OpcionAtributo { get; set; } = null!;
    }
}