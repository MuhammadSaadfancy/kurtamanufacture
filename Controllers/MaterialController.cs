using FashionPro.Data;
using FashionPro.Models;
using FashionPro.Models;
using FashionPro.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FashionPro.Controllers
{
	public class MaterialController : Controller
	{
		private readonly ApplicationDbContext _context;

		public MaterialController(ApplicationDbContext context)
		{
			_context = context;
		}

		private bool IsLoggedIn()
		{
			return HttpContext.Session.GetString("UserId") != null;
		}

		private int? GetCurrentUserId()
		{
			var userId = HttpContext.Session.GetString("UserId");
			return userId != null ? int.Parse(userId) : (int?)null;
		}

		// GET: Material Dashboard
		public async Task<IActionResult> Dashboard()
		{
			if (!IsLoggedIn())
			{
				return RedirectToAction("Login", "Auth");
			}

			var today = DateTime.Today;
			var startOfMonth = new DateTime(today.Year, today.Month, 1);

			var dashboard = new MaterialDashboardVM
			{
				TotalAmount = await _context.Materials.SumAsync(m => m.Amount),
				TotalTransactions = await _context.Materials.CountAsync(),
				TodayAmount = await _context.Materials
					.Where(m => m.Date.Date == today)
					.SumAsync(m => (decimal?)m.Amount) ?? 0,
				MonthAmount = await _context.Materials
					.Where(m => m.Date >= startOfMonth)
					.SumAsync(m => (decimal?)m.Amount) ?? 0,
				TopMaterials = await _context.Materials
					.GroupBy(m => m.MaterialName)
					.Select(g => new MaterialByCategoryVM
					{
						MaterialName = g.Key,
						TotalAmount = g.Sum(m => m.Amount),
						Count = g.Count()
					})
					.OrderByDescending(g => g.TotalAmount)
					.Take(10)
					.ToListAsync()
			};

			return View(dashboard);
		}

		// GET: Material List
		public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate, string searchTerm, int page = 1)
		{
			if (!IsLoggedIn())
			{
				return RedirectToAction("Login", "Auth");
			}

			ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd") ?? DateTime.Now.AddDays(-30).ToString("yyyy-MM-dd");
			ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd") ?? DateTime.Now.ToString("yyyy-MM-dd");
			ViewBag.SearchTerm = searchTerm;

			var query = _context.Materials.AsQueryable();

			if (fromDate.HasValue)
			{
				query = query.Where(m => m.Date.Date >= fromDate.Value.Date);
			}

			if (toDate.HasValue)
			{
				query = query.Where(m => m.Date.Date <= toDate.Value.Date);
			}

			if (!string.IsNullOrEmpty(searchTerm))
			{
				query = query.Where(m => m.MaterialName.Contains(searchTerm) ||
										 (m.Notes != null && m.Notes.Contains(searchTerm)));
			}

			int pageSize = 15;
			int totalRecords = await query.CountAsync();
			int totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

			var materials = await query
				.OrderByDescending(m => m.Date)
				.Skip((page - 1) * pageSize)
				.Take(pageSize)
				.Select(m => new MaterialListVM
				{
					Id = m.Id,
					Date = m.Date,
					MaterialName = m.MaterialName,
					QuantityDisplay = m.Quantity.HasValue ? $"{m.Quantity} {m.Unit}" : "-",
					Amount = m.Amount,
					Notes = m.Notes
				})
				.ToListAsync();

			ViewBag.CurrentPage = page;
			ViewBag.TotalPages = totalPages;
			ViewBag.TotalRecords = totalRecords;
			ViewBag.TotalAmount = await query.SumAsync(m => m.Amount);

			return View(materials);
		}

		// GET: Material Create
		public IActionResult Create()
		{
			if (!IsLoggedIn())
			{
				return RedirectToAction("Login", "Auth");
			}

			var model = new MaterialVM
			{
				Date = DateTime.Now,
				Units = GetUnits()
			};
			return View(model);
		}

		// POST: Material Create
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(MaterialVM model)
		{
			if (!IsLoggedIn())
			{
				return RedirectToAction("Login", "Auth");
			}

			// Debug Info
			ViewBag.IsValid = ModelState.IsValid;
			ViewBag.ErrorCount = ModelState.ErrorCount;

			try
			{
				// 🔥 Force Save (ModelState ignore karke)
				var material = new Material
				{
					Date = model.Date != default(DateTime) ? model.Date : DateTime.Now,
					MaterialName = string.IsNullOrWhiteSpace(model.MaterialName) ? "Untitled Material" : model.MaterialName.Trim(),
					Quantity = model.Quantity > 0 ? model.Quantity : 1,
					Unit = string.IsNullOrWhiteSpace(model.Unit) ? "Pcs" : model.Unit.Trim(),
					Amount = model.Amount > 0 ? model.Amount : 0,
					Notes = model.Notes?.Trim() ?? "",
					CreatedBy = GetCurrentUserId(),
					CreatedAt = DateTime.Now
				};

				_context.Materials.Add(material);
				await _context.SaveChangesAsync();

				TempData["Success"] = $"Material '{material.MaterialName}' of ₨ {material.Amount:N0} added successfully!";
				return RedirectToAction(nameof(Index));
			}
			catch (Exception ex)
			{
				TempData["Error"] = $"Save Error: {ex.Message}";
				Console.WriteLine(ex.ToString()); // Output window mein check karo
				model.Units = GetUnits();
				return View(model);
			}
		}
		// GET: Material Edit
		public async Task<IActionResult> Edit(int id)
		{
			if (!IsLoggedIn())
			{
				return RedirectToAction("Login", "Auth");
			}

			var material = await _context.Materials.FindAsync(id);
			if (material == null)
			{
				TempData["Error"] = "Material record not found!";
				return RedirectToAction(nameof(Index));
			}

			var model = new MaterialVM
			{
				Id = material.Id,
				Date = material.Date,
				MaterialName = material.MaterialName,
				Quantity = material.Quantity,
				Unit = material.Unit,
				Amount = material.Amount,
				Notes = material.Notes,
				Units = GetUnits()
			};

			return View(model);
		}

		// POST: Material Edit
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(int id, MaterialVM model)
		{
			if (!IsLoggedIn())
			{
				return RedirectToAction("Login", "Auth");
			}

			if (id != model.Id)
			{
				return NotFound();
			}

			if (ModelState.IsValid)
			{
				try
				{
					var material = await _context.Materials.FindAsync(id);
					if (material == null)
					{
						TempData["Error"] = "Material record not found!";
						return RedirectToAction(nameof(Index));
					}

					material.Date = model.Date;
					material.MaterialName = model.MaterialName;
					material.Quantity = model.Quantity;
					material.Unit = model.Unit;
					material.Amount = model.Amount;
					material.Notes = model.Notes;

					_context.Update(material);
					await _context.SaveChangesAsync();

					TempData["Success"] = "Material record updated successfully!";
					return RedirectToAction(nameof(Index));
				}
				catch (DbUpdateConcurrencyException)
				{
					if (!MaterialExists(model.Id))
					{
						return NotFound();
					}
					throw;
				}
			}

			model.Units = GetUnits();
			return View(model);
		}

		// GET: Material Delete
		public async Task<IActionResult> Delete(int id)
		{
			if (!IsLoggedIn())
			{
				return RedirectToAction("Login", "Auth");
			}

			var material = await _context.Materials.FindAsync(id);
			if (material == null)
			{
				TempData["Error"] = "Material record not found!";
				return RedirectToAction(nameof(Index));
			}

			return View(material);
		}

		// POST: Material Delete
		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed(int id)
		{
			if (!IsLoggedIn())
			{
				return RedirectToAction("Login", "Auth");
			}

			var material = await _context.Materials.FindAsync(id);
			if (material != null)
			{
				_context.Materials.Remove(material);
				await _context.SaveChangesAsync();
				TempData["Success"] = $"Material '{material.MaterialName}' deleted successfully!";
			}

			return RedirectToAction(nameof(Index));
		}

		// GET: Material Report
		public async Task<IActionResult> Report(string period = "month")
		{
			if (!IsLoggedIn())
			{
				return RedirectToAction("Login", "Auth");
			}

			DateTime startDate;
			switch (period.ToLower())
			{
				case "week":
					startDate = DateTime.Today.AddDays(-7);
					break;
				case "month":
					startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
					break;
				case "year":
					startDate = new DateTime(DateTime.Today.Year, 1, 1);
					break;
				default:
					startDate = DateTime.Today.AddDays(-30);
					break;
			}

			var materials = await _context.Materials
				.Where(m => m.Date >= startDate)
				.OrderByDescending(m => m.Date)
				.ToListAsync();

			var dailySummary = materials
				.GroupBy(m => m.Date.Date)
				.Select(g => new { Date = g.Key, Total = g.Sum(m => m.Amount), Count = g.Count() })
				.OrderByDescending(g => g.Date)
				.Take(30)
				.ToList();

			var categorySummary = materials
				.GroupBy(m => m.MaterialName)
				.Select(g => new MaterialByCategoryVM
				{
					MaterialName = g.Key,
					TotalAmount = g.Sum(m => m.Amount),
					Count = g.Count()
				})
				.OrderByDescending(g => g.TotalAmount)
				.ToList();

			ViewBag.Period = period;
			ViewBag.StartDate = startDate.ToString("dd-MMM-yyyy");
			ViewBag.TotalAmount = materials.Sum(m => m.Amount);
			ViewBag.TotalCount = materials.Count;
			ViewBag.DailySummary = dailySummary;
			ViewBag.CategorySummary = categorySummary;

			return View();
		}

		private List<SelectListItem> GetUnits()
		{
			return new List<SelectListItem>
			{
				new SelectListItem { Value = "KG", Text = "Kilogram (KG)" },
				new SelectListItem { Value = "Piece", Text = "Piece" },
				new SelectListItem { Value = "Roll", Text = "Roll" },
				new SelectListItem { Value = "Packet", Text = "Packet" },
				new SelectListItem { Value = "Meter", Text = "Meter" },
				new SelectListItem { Value = "Yard", Text = "Yard" },
				new SelectListItem { Value = "Dozen", Text = "Dozen" }
			};
		}

		private bool MaterialExists(int id)
		{
			return _context.Materials.Any(e => e.Id == id);
		}
	}
}