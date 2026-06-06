using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FashionPro.Services
{
	public class SmsService : ISmsService
	{
		private readonly IConfiguration _configuration;
		private readonly ILogger<SmsService> _logger;
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly bool _isEnabled;
		private readonly string _apiKey;
		private readonly string _apiSecret;
		private readonly string _from;

		public SmsService(
			IConfiguration configuration,
			ILogger<SmsService> logger,
			IHttpClientFactory httpClientFactory)
		{
			_configuration = configuration;
			_logger = logger;
			_httpClientFactory = httpClientFactory;
			_isEnabled = configuration.GetValue<bool>("SmsSettings:Enabled");
			_apiKey = configuration["SmsSettings:ApiKey"] ?? "";
			_apiSecret = configuration["SmsSettings:ApiSecret"] ?? "";
			_from = configuration["SmsSettings:From"] ?? "TXTLCL";
		}

		public async Task<bool> SendSmsAsync(string toPhoneNumber, string message)
		{
			if (!_isEnabled)
			{
				_logger.LogInformation($"SMS disabled. Would send to {toPhoneNumber}: {message}");
				return true;
			}

			if (string.IsNullOrEmpty(toPhoneNumber))
			{
				_logger.LogWarning("Phone number is empty. SMS not sent.");
				return false;
			}

			if (string.IsNullOrEmpty(_apiKey) || string.IsNullOrEmpty(_apiSecret))
			{
				_logger.LogError("MoceanAPI credentials not configured!");
				return false;
			}

			var formattedNumber = FormatPhoneNumber(toPhoneNumber);

			try
			{
				using var httpClient = _httpClientFactory.CreateClient();

				// ✅ FIXED: Query string format - MoceanAPI accepts GET with query parameters
				var queryParams = new List<string>
				{
					$"mocean-api-key={Uri.EscapeDataString(_apiKey)}",
					$"mocean-api-secret={Uri.EscapeDataString(_apiSecret)}",
					$"mocean-to={Uri.EscapeDataString(formattedNumber)}",
					$"mocean-text={Uri.EscapeDataString(message)}",
					$"mocean-from={Uri.EscapeDataString(_from)}"
				};

				var url = "https://rest.moceanapi.com/rest/2/sms?" + string.Join("&", queryParams);

				_logger.LogInformation($"Sending SMS to {formattedNumber}");
				_logger.LogInformation($"Request URL: https://rest.moceanapi.com/rest/2/sms?mocean-api-key=***&mocean-api-secret=***&mocean-to={formattedNumber}&mocean-from={_from}");

				var response = await httpClient.GetAsync(url);
				var result = await response.Content.ReadAsStringAsync();

				_logger.LogInformation($"MoceanAPI Response: {result}");

				// ✅ Check XML response for success status
				if (result.Contains("<status>0</status>"))
				{
					_logger.LogInformation("SMS sent successfully!");
					return true;
				}

				// Extract error message if any
				if (result.Contains("<err_msg>"))
				{
					int start = result.IndexOf("<err_msg>") + 9;
					int end = result.IndexOf("</err_msg>");
					string errorMsg = result.Substring(start, end - start);
					_logger.LogError($"MoceanAPI Error: {errorMsg}");
				}

				return false;
			}
			catch (Exception ex)
			{
				_logger.LogError($"MoceanAPI SMS failed: {ex.Message}");
				return false;
			}
		}

		private string FormatPhoneNumber(string phone)
		{
			var cleaned = new string(phone.Where(char.IsDigit).ToArray());

			if (cleaned.StartsWith("0"))
			{
				cleaned = "92" + cleaned.Substring(1);
			}
			else if (!cleaned.StartsWith("92") && cleaned.Length == 10)
			{
				cleaned = "92" + cleaned;
			}

			return cleaned;
		}

		public async Task<bool> SendPaymentReceivedSms(string customerPhone, string customerName, decimal amount, decimal remainingBalance)
		{
			string message = $@"FASHIONPRO - Payment Received

Dear {customerName},

Amount Received: Rs. {amount:N0}
Remaining Balance: Rs. {remainingBalance:N0}

Thank you for your payment!

FashionPro Team";

			return await SendSmsAsync(customerPhone, message);
		}

		public async Task<bool> SendPaymentSentSms(string partyPhone, string partyName, decimal amount, decimal remainingBalance, string moduleType)
		{
			string message = $@"FASHIONPRO - Payment Sent

Dear {partyName},

Module: {moduleType}
Amount Sent: Rs. {amount:N0}
Remaining Balance: Rs. {remainingBalance:N0}

Thank you for your service!

FashionPro Team";

			return await SendSmsAsync(partyPhone, message);
		}

		public async Task<bool> SendBillReminderSms(string customerPhone, string customerName, decimal billAmount, decimal pendingAmount)
		{
			string message = $@"FASHIONPRO - Payment Reminder

Dear {customerName},

Bill Amount: Rs. {billAmount:N0}
Pending Amount: Rs. {pendingAmount:N0}

Please clear your pending payment.

FashionPro Team";

			return await SendSmsAsync(customerPhone, message);
		}

		public async Task<bool> TestSms(string phoneNumber)
		{
			string message = $@"FASHIONPRO - Test SMS

Your SMS system is working successfully!

Test Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}

FashionPro Team";

			return await SendSmsAsync(phoneNumber, message);
		}
	}
}