namespace FashionPro.Services
{
	public interface ISmsService
	{
		Task<bool> SendSmsAsync(string toPhoneNumber, string message);
		Task<bool> SendPaymentReceivedSms(string customerPhone, string customerName, decimal amount, decimal remainingBalance);
		Task<bool> SendPaymentSentSms(string partyPhone, string partyName, decimal amount, decimal remainingBalance, string moduleType);
		Task<bool> SendBillReminderSms(string customerPhone, string customerName, decimal billAmount, decimal pendingAmount);
		Task<bool> TestSms(string phoneNumber);
	}
}