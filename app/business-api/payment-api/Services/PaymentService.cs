using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


    public class PaymentService : IPaymentService
{
        private readonly ILogger<PaymentService> _logger;
        private readonly HttpClient _httpClient;
        private readonly string _transactionApiUrl;

        public PaymentService(
            ILogger<PaymentService> logger,
            HttpClient httpClient,
            string transactionApiURL)
        {
            _logger = logger;
            _httpClient = httpClient;
            _transactionApiUrl = transactionApiURL;
        }

        public async Task ProcessPaymentAsync(Payment payment)
        {
            // Validate AccountId
            if (string.IsNullOrEmpty(payment.AccountId))
                throw new ArgumentException("AccountId is empty or null");

            if (!int.TryParse(payment.AccountId, out _))
                throw new ArgumentException("AccountId is not a valid number");

            // Validate PaymentMethodId
            if (payment.PaymentType?.ToLower() != "transfer" &&
                string.IsNullOrEmpty(payment.PaymentMethodId))
                throw new ArgumentException("paymentMethodId is empty or null");

            if (!int.TryParse(payment.PaymentMethodId, out _))
                throw new ArgumentException("paymentMethodId is not a valid number");

            // Log payment details
            _logger.LogInformation($"Payment successful for: {payment}");

            // Convert Payment to Transaction
            var transaction = ConvertPaymentToTransaction(payment);

            // Send transaction to API
            _logger.LogInformation($"Notifying payment [{payment.Description}] for account[{transaction.AccountId}]");

            try
            {
                var response = await _httpClient.PostAsJsonAsync(
                    $"{_transactionApiUrl}/transactions/{payment.AccountId}",
                    transaction
                );

                response.EnsureSuccessStatusCode();
                var responseBody = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"Transaction notified for: {transaction}");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex,
                    "Error notifying transaction for account {AccountId}",
                    payment.AccountId
                );
                throw;
            }
        }

    private Transaction ConvertPaymentToTransaction(Payment payment)
    {
        return new Transaction() { 
            Id = Guid.NewGuid().ToString(),
            Description = payment.Description,
            Type = "outcome",
            RecipientName = payment.RecipientName,
            RecipientBankCode = payment.RecipientBankCode,
            PaymentType = payment.PaymentType,
            AccountId = payment.AccountId,
            Amount = payment.Amount,
            Timestamp = payment.Timestamp
        };
    }
    }
