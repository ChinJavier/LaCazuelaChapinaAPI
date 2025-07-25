using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LaCazuelaChapina.API.Models.Enums;
using LaCazuelaChapina.API.Models.Sucursales;

namespace LaCazuelaChapina.API.Models.Inventario
{
    [Table("movimientos_inventario")]
    public class MovimientoInventario
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("sucursal_id")]
        public int SucursalId { get; set; }

        [Column("materia_prima_id")]
        public int MateriaPrimaId { get; set; }

        [Column("tipo_movimiento")]
        public TipoMovimiento TipoMovimiento { get; set; }

        [Column("cantidad", TypeName = "decimal(10,3)")]
        public decimal Cantidad { get; set; }

        [Column("costo_unitario", TypeName = "decimal(10,4)")]
        [Range(0, 999999.9999, ErrorMessage = "El costo unitario debe ser mayor o igual a 0")]
        public decimal? CostoUnitario { get; set; }

        [Column("monto_total", TypeName = "decimal(10,2)")]
        [Range(0, 999999.99, ErrorMessage = "El monto total debe ser mayor o igual a 0")]
        public decimal? MontoTotal { get; set; }

        [MaxLength(200, ErrorMessage = "El motivo no puede exceder 200 caracteres")]
        [Column("motivo")]
        public string? Motivo { get; set; }

        [MaxLength(50, ErrorMessage = "El documento de referencia no puede exceder 50 caracteres")]
        [Column("documento_referencia")]
        public string? DocumentoReferencia { get; set; }

        [Column("fecha_movimiento")]
        public DateTime FechaMovimiento { get; set; } = DateTime.UtcNow;

        [Column("usuario_id")]
        public int? UsuarioId { get; set; }

        // Navegación
        public virtual Sucursal Sucursal { get; set; } = null!;
        public virtual MateriaPrima MateriaPrima { get; set; } = null!;
    }
}