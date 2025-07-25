using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LaCazuelaChapina.API.Models.Sucursales;

namespace LaCazuelaChapina.API.Models.Inventario
{
    [Table("stock_sucursal")]
    public class StockSucursal
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("sucursal_id")]
        public int SucursalId { get; set; }

        [Column("materia_prima_id")]
        public int MateriaPrimaId { get; set; }

        [Column("cantidad_actual", TypeName = "decimal(10,3)")]
        [Range(0, 999999.999, ErrorMessage = "La cantidad actual debe ser mayor o igual a 0")]
        public decimal CantidadActual { get; set; } = 0;

        [Column("fecha_ultima_actualizacion")]
        public DateTime FechaUltimaActualizacion { get; set; } = DateTime.UtcNow;

        // Navegación
        public virtual Sucursal Sucursal { get; set; } = null!;
        public virtual MateriaPrima MateriaPrima { get; set; } = null!;
    }
}