using FashionPro.Services;
using Microsoft.AspNetCore.Mvc;

namespace FashionPro.Controllers
{
	public class TestSmsController : Controller
	{
		private readonly ISmsService _smsService;

		public TestSmsController(ISmsService smsService)
		{
			_smsService = smsService;
		}

		[HttpGet]
		public IActionResult Index()
		{
			return View();
		}

		[HttpPost]
		public async Task<IActionResult> SendTest(string phoneNumber)
		{
			if (string.IsNullOrEmpty(phoneNumber))
			{
				ViewBag.Error = "Phone number is required!";
				return View("Index");
			}

			var result = await _smsService.TestSms(phoneNumber);

			if (result)
			{
				ViewBag.Success = $"✅ Test SMS sent successfully to {phoneNumber}!";
			}
			else
			{
				ViewBag.Error = "❌ Failed to send SMS. Check API credentials.";
			}

			return View("Index");
		}
	}
}