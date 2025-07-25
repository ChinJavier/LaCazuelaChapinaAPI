using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LaCazuelaChapina.API.Models.Ventas;
using LaCazuelaChapina.API.Models.Inventario;
using LaCazuelaChapina.API.Models.Notificaciones;

namespace LaCazuelaChapina.API.Models.Sucursales
{
    [Table("sucursales")]
    public class Sucursal
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre de la sucursal es obligatorio")]
        [MaxLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        [Column("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [MaxLength(200, ErrorMessage = "La dirección no puede exceder 200 caracteres")]
        [Column("direccion")]
        public string? Direccion { get; set; }

        [MaxLength(15, ErrorMessage = "El teléfono no puede exceder 15 caracteres")]
        [Phone(ErrorMessage = "Formato de teléfono inválido")]
        [Column("telefono")]
        public string? Telefono { get; set; }

        [MaxLength(100, ErrorMessage = "El email no puede exceder 100 caracteres")]
        [EmailAddress(ErrorMessage = "Formato de email inválido")]
        [Column("email")]
        public string? Email { get; set; }

        [Column("fecha_apertura")]
        public DateTime? FechaApertura { get; set; }

        [Column("activa")]
        public bool Activa { get; set; } = true;

        [Column("fecha_creacion")]
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        [Column("fecha_actualizacion")]
        public DateTime FechaActualizacion { get; set; } = DateTime.UtcNow;

        // Navegación
        public virtual ICollection<Venta> Ventas { get; set; } = new List<Venta>();
        public virtual ICollection<StockSucursal> Stocks { get; set; } = new List<StockSucursal>();
        public virtual ICollection<MovimientoInventario> MovimientosInventario { get; set; } = new List<MovimientoInventario>();
        public virtual ICollection<Notificacion> Notificaciones { get; set; } = new List<Notificacion>();
    }
}