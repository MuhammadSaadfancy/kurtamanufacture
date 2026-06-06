using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace FashionPro.Services
{
	public class FileUploadService : IFileUploadService
	{
		private readonly IWebHostEnvironment _webHostEnvironment;

		public FileUploadService(IWebHostEnvironment webHostEnvironment)
		{
			_webHostEnvironment = webHostEnvironment;
		}

		public async Task<string> UploadPaymentImage(IFormFile file, string paymentType)
		{
			if (file == null || file.Length == 0)
				return "";

			try
			{
				string uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "payments");

				if (!Directory.Exists(uploadPath))
				{
					Directory.CreateDirectory(uploadPath);
				}

				string fileName = $"{paymentType}_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString().Substring(0, 8)}{Path.GetExtension(file.FileName)}";
				string filePath = Path.Combine(uploadPath, fileName);

				using (var stream = new FileStream(filePath, FileMode.Create))
				{
					await file.CopyToAsync(stream);
				}

				return $"/uploads/payments/{fileName}";
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Image upload failed: {ex.Message}");
				return "";
			}
		}

		public void DeletePaymentImage(string imagePath)
		{
			if (string.IsNullOrEmpty(imagePath))
				return;

			try
			{
				string fullPath = Path.Combine(_webHostEnvironment.WebRootPath, imagePath.TrimStart('/'));
				if (System.IO.File.Exists(fullPath))
				{
					System.IO.File.Delete(fullPath);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Image delete failed: {ex.Message}");
			}
		}
	}
}