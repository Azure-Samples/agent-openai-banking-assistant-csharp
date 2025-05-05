/// <summary>
/// Interface for processing payment requests.
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Processes a payment request asynchronously.
    /// </summary>
    /// <param name="payment">The payment details to process.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ProcessPaymentAsync(Payment payment);
}

