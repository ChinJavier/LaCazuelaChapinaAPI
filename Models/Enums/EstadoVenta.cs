namespace LaCazuelaChapina.API.Models.Enums
{
    /// <summary>
    /// Estados posibles de una venta
    /// </summary>
    public enum EstadoVenta
    {
        /// <summary>
        /// Venta creada pero no completada
        /// </summary>
        Pendiente,
        
        /// <summary>
        /// Venta finalizada exitosamente
        /// </summary>
        Completada,
        
        /// <summary>
        /// Venta cancelada o anulada
        /// </summary>
        Cancelada
    }
}