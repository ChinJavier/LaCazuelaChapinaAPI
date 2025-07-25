namespace LaCazuelaChapina.API.Models.Enums
{
    /// <summary>
    /// Métodos de pago aceptados
    /// </summary>
    public enum TipoPago
    {
        /// <summary>
        /// Pago en efectivo
        /// </summary>
        Efectivo,
        
        /// <summary>
        /// Pago con tarjeta de crédito/débito
        /// </summary>
        Tarjeta,
        
        /// <summary>
        /// Transferencia bancaria o digital
        /// </summary>
        Transferencia,
        
        /// <summary>
        /// Combinación de métodos de pago
        /// </summary>
        Mixto
    }
}