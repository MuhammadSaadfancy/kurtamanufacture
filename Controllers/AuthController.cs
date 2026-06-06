using System.Security.Cryptography;
using System.Text;
using FashionPro.Data;
using FashionPro.Models;
using FashionPro.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FashionPro.Controllers
{
	public class AuthController : Controller
	{
		private readonly ApplicationDbContext _context;

		public AuthController(ApplicationDbContext context)
		{
			_context = context;
		}

		[HttpGet]
		public IActionResult Login()
		{
			if (HttpContext.Session.GetString("UserId") != null)
			{
				return RedirectToAction("Dashboard", "Report");
			}
			return View();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Login(LoginVM model)
		{
			if (ModelState.IsValid)
			{
				var user = await _context.Users
					.FirstOrDefaultAsync(u => u.Username == model.Username && u.IsActive == true);

				if (user != null)
				{
					// 🔥 Full namespace
					if (BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
					{
						HttpContext.Session.SetString("UserId", user.UserId.ToString());
						HttpContext.Session.SetString("Username", user.Username);
						HttpContext.Session.SetString("FullName", user.FullName ?? user.Username);

						TempData["Success"] = "Welcome " + user.FullName + "!";
						return RedirectToAction("Dashboard", "Report");
					}
				}

				ModelState.AddModelError("", "Invalid username or password");
			}
			return View(model);
		}

		[HttpGet]
		public IActionResult Logout()
		{
			HttpContext.Session.Clear();
			return RedirectToAction("Login");
		}

		//[HttpGet]
		//public async Task<IActionResult> SetupAdmin()
		//{
		//	var adminExists = await _context.Users.AnyAsync(u => u.Username == "admin");

		//	if (!adminExists)
		//	{
		//		var admin = new User
		//		{
		//			Username = "admin",
		//			PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
		//			FullName = "System Administrator",
		//			IsActive = true,
		//			CreatedAt = DateTime.Now
		//		};

		//		_context.Users.Add(admin);
		//		await _context.SaveChangesAsync();

		//		TempData["Success"] = "Admin created! Username: admin, Password: admin123";
		//	}
		//	else
		//	{
		//		var admin = await _context.Users.FirstOrDefaultAsync(u => u.Username == "admin");
		//		if (admin != null)
		//		{
		//			admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123");
		//			await _context.SaveChangesAsync();
		//			TempData["Success"] = "Admin password reset! Username: admin, Password: admin123";
		//		}
		//	}

		//	return RedirectToAction("Login");
		//}

		//[HttpGet]
		//public async Task<IActionResult> ResetFancyPassword()
		//{
		//	var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == "fancy");
		//	if (user != null)
		//	{
		//		user.PasswordHash = BCrypt.Net.BCrypt.HashPassword("fancy123");
		//		await _context.SaveChangesAsync();
		//		return Content("Fancy user password reset to: fancy123");
		//	}
		//	return Content("Fancy user not found");
		//}

		[HttpGet]
		public async Task<IActionResult> ShowUsers()
		{
			var users = await _context.Users.ToListAsync();
			var result = "";
			foreach (var u in users)
			{
				result += $"{u.Username} - {u.FullName}<br/>";
			}
			return Content(result);
		}

		[HttpGet]
		public IActionResult ChangePassword()
		{
			if (HttpContext.Session.GetString("UserId") == null)
			{
				return RedirectToAction("Login");
			}
			return View();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ChangePassword(ChangePasswordVM model)
		{
			if (HttpContext.Session.GetString("UserId") == null)
			{
				return RedirectToAction("Login");
			}

			if (ModelState.IsValid)
			{
				var userId = int.Parse(HttpContext.Session.GetString("UserId"));
				var user = await _context.Users.FindAsync(userId);

				if (user != null)
				{
					if (BCrypt.Net.BCrypt.Verify(model.CurrentPassword, user.PasswordHash))
					{
						user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
						await _context.SaveChangesAsync();

						TempData["Success"] = "Password changed successfully! Please login again.";
						return RedirectToAction("Logout");
					}
					else
					{
						ModelState.AddModelError("", "Current password is incorrect");
					}
				}
			}
			return View(model);
		}
	}
}