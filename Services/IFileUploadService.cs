using Microsoft.AspNetCore.Http;

namespace FashionPro.Services
{
	public interface IFileUploadService
	{
		Task<string> UploadPaymentImage(IFormFile file, string paymentType);
		void DeletePaymentImage(string imagePath);
	}
}