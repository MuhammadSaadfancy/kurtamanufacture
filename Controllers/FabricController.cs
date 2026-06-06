using FashionPro.Data;
using FashionPro.Models;
using FashionPro.Models.ViewModels;
using FashionPro.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FashionPro.Controllers
{
	public class FabricController : Controller
	{
		private readonly ApplicationDbContext _context;
		private readonly IBalanceService _balanceService;
		private readonly IFileUploadService _fileUploadService;
		private readonly ISmsService _smsService;  // 🔥 ADDED

		public FabricController(ApplicationDbContext context, IBalanceService balanceService, IFileUploadService fileUploadService, ISmsService smsService)  // 🔥 ADDED
		{
			_context = context;
			_balanceService = balanceService;
			_fileUploadService = fileUploadService;
			_smsService = smsService;  // 🔥 ADDED
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

		// GET: Fabric Dashboard
		public async Task<IActionResult> Dashboard()
		{
			if (!IsLoggedIn())
			{
				return RedirectToAction("Login", "Auth");
			}

			var dashboard = new FabricDashboardVM
			{
				TotalFabricAmount = await _context.Fabrics.SumAsync(f => (decimal?)f.Amount) ?? 0,
				TotalPaid = await _context.FabricPayments.SumAsync(p => (decimal?)p.Amount) ?? 0,
				TotalTransactions = await _context.Fabrics.CountAsync(),
				SupplierBalances = await _context.Parties
					.Where(p => p.PartyType == "Supplier" && p.IsActive)
					.Select(p => new SupplierBalanceVM
					{
						PartyId = p.PartyId,
						PartyName = p.PartyName ?? "",
						TotalAmount = _context.Fabrics.Where(f => f.PartyId == p.PartyId).Sum(f => (decimal?)f.Amount) ?? 0,
						TotalPaid = _context.FabricPayments.Where(fp => fp.PartyId == p.PartyId).Sum(fp => (decimal?)fp.Amount) ?? 0,
						Balance = (_context.Fabrics.Where(f => f.PartyId == p.PartyId).Sum(f => (decimal?)f.Amount) ?? 0)
							   - (_context.FabricPayments.Where(fp => fp.PartyId == p.PartyId).Sum(fp => (decimal?)fp.Amount) ?? 0)
					})
					.Where(s => s.TotalAmount > 0 || s.TotalPaid > 0)
					.OrderByDescending(s => s.Balance)
					.ToListAsync()
			};

			dashboard.TotalBalance = dashboard.TotalFabricAmount - dashboard.TotalPaid;

			return View(dashboard);
		}

		// GET: Fabric List
		public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate, int? partyId, int page = 1)
		{
			if (!IsLoggedIn())
			{
				return RedirectToAction("Login", "Auth");
			}

			ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd") ?? DateTime.Now.AddDays(-30).ToString("yyyy-MM-dd");
			ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd") ?? DateTime.Now.ToString("yyyy-MM-dd");
			ViewBag.SelectedParty = partyId;

			var suppliers = await _context.Parties
				.Where(p => p.PartyType == "Supplier" && p.IsActive)
				.Select(p => new SelectListItem { Value = p.PartyId.ToString(), Text = p.PartyName ?? "" })
				.ToListAsync();
			suppliers.Insert(0, new SelectListItem { Value = "", Text = "-- All Suppliers --" });
			ViewBag.Suppliers = suppliers;

			var query = _context.Fabrics
				.Include(f => f.Party)
				.AsQueryable();

			if (fromDate.HasValue)
				query = query.Where(f => f.Date.Date >= fromDate.Value.Date);

			if (toDate.HasValue)
				query = query.Where(f => f.Date.Date <= toDate.Value.Date);

			if (partyId.HasValue && partyId > 0)
				query = query.Where(f => f.PartyId == partyId.Value);

			int pageSize = 15;
			int totalRecords = await query.CountAsync();
			int totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

			var allFabrics = await _context.Fabrics.ToListAsync();
			decimal totalFabricAmount = allFabrics.Sum(f => f.Amount);
			decimal totalPaid = await _context.FabricPayments.SumAsync(p => (decimal?)p.Amount) ?? 0;
			decimal totalPending = totalFabricAmount - totalPaid;

			ViewBag.GrandTotalAmount = totalFabricAmount;
			ViewBag.GrandTotalPending = totalPending;

			var fabrics = await query
				.OrderByDescending(f => f.Date)
				.Skip((page - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			var fabricList = new List<FabricListVM>();

			foreach (var fabric in fabrics)
			{
				var supplierTotalPaid = await _context.FabricPayments
					.Where(p => p.PartyId == fabric.PartyId)
					.SumAsync(p => (decimal?)p.Amount) ?? 0;

				var balance = fabric.Amount - supplierTotalPaid;

				fabricList.Add(new FabricListVM
				{
					Id = fabric.Id,
					Date = fabric.Date,
					PartyId = fabric.PartyId,
					PartyName = fabric.Party?.PartyName ?? "",
					Variety = fabric.Variety ?? "",
					Quantity = fabric.KG.HasValue ? $"{fabric.KG} KG" : (fabric.Than.HasValue ? $"{fabric.Than} Than" : "-"),
					Amount = fabric.Amount,
					Balance = balance,
					Notes = fabric.Notes ?? ""
				});
			}

			ViewBag.CurrentPage = page;
			ViewBag.TotalPages = totalPages;
			ViewBag.TotalRecords = totalRecords;
			ViewBag.TotalAmount = totalFabricAmount;

			return View(fabricList);
		}

		// GET: Fabric Create
		public async Task<IActionResult> Create()
		{
			if (!IsLoggedIn())
			{
				return RedirectToAction("Login", "Auth");
			}

			var model = new FabricVM
			{
				Date = DateTime.Now,
				Suppliers = await GetSuppliers()
			};
			return View(model);
		}

		// POST: Fabric Create
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(FabricVM model)
		{
			if (!IsLoggedIn())
			{
				return RedirectToAction("Login", "Auth");
			}

			try
			{
				var fabric = new Fabric
				{
					Date = model.Date != default ? model.Date : DateTime.Now,
					PartyId = model.PartyId > 0 ? model.PartyId : 1,
					Variety = string.IsNullOrWhiteSpace(model.Variety) ? "No Variety" : model.Variety.Trim(),
					KG = model.KG > 0 ? model.KG : 0,
					Than = model.Than > 0 ? model.Than : 0,
					Amount = model.Amount > 0 ? model.Amount : 0,
					Balance = model.Amount > 0 ? model.Amount : 0,
					Notes = model.Notes?.Trim() ?? "",
					CreatedBy = GetCurrentUserId(),
					CreatedAt = DateTime.Now
				};

				_context.Fabrics.Add(fabric);
				await _context.SaveChangesAsync();

				await _balanceService.UpdatePartyBalance(fabric.PartyId);

				TempData["Success"] = $"✅ Fabric entry saved successfully! ID: {fabric.Id}";
				return RedirectToAction(nameof(Index));
			}
			catch (Exception ex)
			{
				TempData["Error"] = $"Save failed: {ex.Message}";
				model.Suppliers = await GetSuppliers();
				return View(model);
			}
		}

		// GET: Fabric Edit
		public async Task<IActionResult> Edit(int id)
		{
			if (!IsLoggedIn())
			{
				return RedirectToAction("Login", "Auth");
			}

			var fabric = await _context.Fabrics.FindAsync(id);
			if (fabric == null)
			{
				TempData["Error"] = "Fabric record not found!";
				return RedirectToAction(nameof(Index));
			}

			var model = new FabricVM
			{
				Id = fabric.Id,
				Date = fabric.Date,
				PartyId = fabric.PartyId,
				Variety = fabric.Variety ?? "",
				KG = fabric.KG,
				Than = fabric.Than,
				Amount = fabric.Amount,
				Balance = fabric.Balance,
				Notes = fabric.Notes ?? "",
				Suppliers = await GetSuppliers()
			};

			return View(model);
		}

		// POST: Fabric Edit
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(int id, FabricVM model)
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
					var fabric = await _context.Fabrics.FindAsync(id);
					if (fabric == null)
					{
						TempData["Error"] = "Fabric record not found!";
						return RedirectToAction(nameof(Index));
					}

					var oldPartyId = fabric.PartyId;

					fabric.Date = model.Date;
					fabric.PartyId = model.PartyId;
					fabric.Variety = model.Variety ?? "";
					fabric.KG = model.KG;
					fabric.Than = model.Than;
					fabric.Amount = model.Amount;
					fabric.Notes = model.Notes ?? "";

					var totalPaid = await _context.FabricPayments
						.Where(p => p.FabricId == id)
						.SumAsync(p => (decimal?)p.Amount) ?? 0;
					fabric.Balance = model.Amount - totalPaid;

					_context.Update(fabric);
					await _context.SaveChangesAsync();

					if (oldPartyId != model.PartyId)
					{
						await _balanceService.UpdatePartyBalance(oldPartyId);
					}
					await _balanceService.UpdatePartyBalance(model.PartyId);

					TempData["Success"] = "Fabric record updated successfully!";
					return RedirectToAction(nameof(Index));
				}
				catch (DbUpdateConcurrencyException)
				{
					if (!FabricExists(model.Id))
					{
						return NotFound();
					}
					throw;
				}
			}

			model.Suppliers = await GetSuppliers();
			return View(model);
		}

		// GET: Fabric Delete
		public async Task<IActionResult> Delete(int id)
		{
			if (!IsLoggedIn())
			{
				return RedirectToAction("Login", "Auth");
			}

			var fabric = await _context.Fabrics
				.Include(f => f.Party)
				.Include(f => f.FabricPayments)
				.FirstOrDefaultAsync(f => f.Id == id);

			if (fabric == null)
			{
				TempData["Error"] = "Fabric record not found!";
				return RedirectToAction(nameof(Index));
			}

			ViewBag.HasPayments = fabric.FabricPayments.Any();
			ViewBag.PaymentCount = fabric.FabricPayments.Count();
			ViewBag.TotalPaid = fabric.FabricPayments.Sum(p => p.Amount);

			return View(fabric);
		}

		// POST: Fabric Delete
		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed(int id)
		{
			if (!IsLoggedIn())
			{
				return RedirectToAction("Login", "Auth");
			}

			var fabric = await _context.Fabrics
				.Include(f => f.FabricPayments)
				.FirstOrDefaultAsync(f => f.Id == id);

			if (fabric != null)
			{
				var partyId = fabric.PartyId;

				if (fabric.FabricPayments.Any())
				{
					_context.FabricPayments.RemoveRange(fabric.FabricPayments);
				}

				_context.Fabrics.Remove(fabric);
				await _context.SaveChangesAsync();

				await _balanceService.UpdatePartyBalance(partyId);

				TempData["Success"] = "Fabric record deleted successfully!";
			}

			return RedirectToAction(nameof(Index));
		}

		// GET: Payments List
		public async Task<IActionResult> Payments(DateTime? fromDate, DateTime? toDate, int? partyId, int page = 1)
		{
			if (!IsLoggedIn())
			{
				return RedirectToAction("Login", "Auth");
			}

			ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd") ?? DateTime.Now.AddDays(-30).ToString("yyyy-MM-dd");
			ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd") ?? DateTime.Now.ToString("yyyy-MM-dd");
			ViewBag.SelectedParty = partyId;

			var suppliers = await _context.Parties
				.Where(p => p.PartyType == "Supplier" && p.IsActive)
				.Select(p => new SelectListItem { Value = p.PartyId.ToString(), Text = p.PartyName ?? "" })
				.ToListAsync();
			suppliers.Insert(0, new SelectListItem { Value = "", Text = "-- All Suppliers --" });
			ViewBag.Suppliers = suppliers;

			var query = _context.FabricPayments
				.Include(p => p.Party)
				.Include(p => p.Fabric)
				.AsQueryable();

			if (fromDate.HasValue)
				query = query.Where(p => p.Date.Date >= fromDate.Value.Date);

			if (toDate.HasValue)
				query = query.Where(p => p.Date.Date <= toDate.Value.Date);

			if (partyId.HasValue && partyId > 0)
				query = query.Where(p => p.PartyId == partyId.Value);

			int pageSize = 15;
			int totalRecords = await query.CountAsync();
			int totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

			var payments = await query
				.OrderByDescending(p => p.Date)
				.Skip((page - 1) * pageSize)
				.Take(pageSize)
				.Select(p => new FabricPaymentListVM
				{
					Id = p.Id,
					Date = p.Date,
					PartyName = p.Party.PartyName ?? "",
					Amount = p.Amount,
					PaymentMethod = p.PaymentMethod ?? "Cash",
					FabricReference = p.Fabric != null ? $"Fabric #{p.FabricId}" : "General Payment",
					Notes = p.Notes ?? "",
					PaymentImagePath = p.PaymentImagePath ?? ""
				})
				.ToListAsync();

			ViewBag.CurrentPage = page;
			ViewBag.TotalPages = totalPages;
			ViewBag.TotalRecords = totalRecords;
			ViewBag.TotalAmount = await query.SumAsync(p => (decimal?)p.Amount) ?? 0;

			return View(payments);
		}

		// GET: Get Fabrics by Supplier (AJAX)
		[HttpGet]
		public async Task<IActionResult> GetFabricsBySupplier(int supplierId)
		{
			var fabrics = await _context.Fabrics
				.Where(f => f.PartyId == supplierId)
				.Select(f => new
				{
					id = f.Id,
					text = $"Fabric #{f.Id} - {f.Variety ?? "No Variety"} - Balance: ₨ {f.Balance:N0}"
				})
				.ToListAsync();

			return Json(fabrics);
		}

		// GET: Create Payment
		public async Task<IActionResult> CreatePayment(int? fabricId, int? partyId)
		{
			if (!IsLoggedIn())
			{
				return RedirectToAction("Login", "Auth");
			}

			var model = new FabricPaymentVM
			{
				Date = DateTime.Now,
				PaymentMethods = GetPaymentMethods(),
				Suppliers = await GetSuppliers()
			};

			if (fabricId.HasValue)
			{
				var fabric = await _context.Fabrics.FindAsync(fabricId.Value);
				if (fabric != null)
				{
					model.FabricId = fabric.Id;
					model.PartyId = fabric.PartyId;
					var party = await _context.Parties.FindAsync(fabric.PartyId);
					model.PartyName = party?.PartyName ?? "";
					model.FabricReference = $"Fabric #{fabric.Id} - {fabric.Variety ?? ""} (Current Balance: ₨ {fabric.Balance:N0})";
				}
			}

			if (partyId.HasValue && !fabricId.HasValue)
			{
				model.PartyId = partyId.Value;
				var party = await _context.Parties.FindAsync(partyId.Value);
				model.PartyName = party?.PartyName ?? "";
			}

			return View(model);
		}

		// POST: Create Payment - 🔥 WITH SMS
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> CreatePayment(FabricPaymentVM model)
		{
			if (!IsLoggedIn())
			{
				return RedirectToAction("Login", "Auth");
			}

			try
			{
				if (model.PartyId == 0)
				{
					TempData["Error"] = "Please select a supplier!";
					model.PaymentMethods = GetPaymentMethods();
					model.Suppliers = await GetSuppliers();
					return View(model);
				}

				if (model.Amount <= 0)
				{
					TempData["Error"] = "Payment amount must be greater than 0!";
					model.PaymentMethods = GetPaymentMethods();
					model.Suppliers = await GetSuppliers();
					return View(model);
				}

				string imagePath = "";
				if (model.PaymentImage != null && model.PaymentImage.Length > 0)
				{
					imagePath = await _fileUploadService.UploadPaymentImage(model.PaymentImage, "fabric_payment");
				}

				var payment = new FabricPayment
				{
					Date = model.Date,
					PartyId = model.PartyId,
					FabricId = null,
					Amount = model.Amount,
					PaymentMethod = model.PaymentMethod ?? "Cash",
					Notes = model.Notes ?? "",
					PaymentImagePath = imagePath,
					CreatedBy = GetCurrentUserId(),
					CreatedAt = DateTime.Now
				};

				_context.FabricPayments.Add(payment);
				await _context.SaveChangesAsync();

				await _balanceService.UpdatePartyBalance(model.PartyId);

				// 🔥 ========== SMS CODE - YAHAN ADD KIYA ========== 🔥
				try
				{
					var party = await _context.Parties.FindAsync(model.PartyId);
					if (party != null && !string.IsNullOrEmpty(party.Phone))
					{
						// Get supplier ka current balance after payment
						var totalFabric = await _context.Fabrics
							.Where(f => f.PartyId == model.PartyId)
							.SumAsync(f => f.Amount);
						var totalPaidToSupplier = await _context.FabricPayments
							.Where(p => p.PartyId == model.PartyId)
							.SumAsync(p => p.Amount);
						var remainingBalance = totalFabric - totalPaidToSupplier;

						await _smsService.SendPaymentSentSms(
							party.Phone,
							party.PartyName,
							model.Amount,
							remainingBalance,
							"Fabrics"
						);
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine($"SMS failed: {ex.Message}");
				}
				// 🔥 ========== SMS CODE END ========== 🔥

				TempData["Success"] = $"✅ Payment of ₨ {model.Amount:N0} recorded successfully!";
				return RedirectToAction(nameof(Payments));
			}
			catch (Exception ex)
			{
				TempData["Error"] = $"Save failed: {ex.Message}";
				model.PaymentMethods = GetPaymentMethods();
				model.Suppliers = await GetSuppliers();
				return View(model);
			}
		}

		// GET: Delete Payment
		public async Task<IActionResult> DeletePayment(int id)
		{
			if (!IsLoggedIn())
			{
				return RedirectToAction("Login", "Auth");
			}

			try
			{
				var payment = await _context.FabricPayments
					.Include(p => p.Party)
					.Include(p => p.Fabric)
					.FirstOrDefaultAsync(p => p.Id == id);

				if (payment == null)
				{
					TempData["Error"] = "Payment record not found!";
					return RedirectToAction(nameof(Payments));
				}

				ViewBag.Fabric = payment.Fabric;

				return View(payment);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error in DeletePayment: {ex.Message}");
				TempData["Error"] = "Error loading payment record";
				return RedirectToAction(nameof(Payments));
			}
		}

		// POST: Delete Payment
		[HttpPost, ActionName("DeletePayment")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeletePaymentConfirmed(int id)
		{
			if (!IsLoggedIn())
			{
				return RedirectToAction("Login", "Auth");
			}

			var payment = await _context.FabricPayments.FindAsync(id);
			if (payment != null)
			{
				var fabricId = payment.FabricId;
				var partyId = payment.PartyId;

				if (!string.IsNullOrEmpty(payment.PaymentImagePath))
				{
					_fileUploadService.DeletePaymentImage(payment.PaymentImagePath);
				}

				_context.FabricPayments.Remove(payment);
				await _context.SaveChangesAsync();

				if (fabricId.HasValue)
				{
					var fabric = await _context.Fabrics.FindAsync(fabricId.Value);
					if (fabric != null)
					{
						var totalPaid = await _context.FabricPayments
							.Where(p => p.FabricId == fabricId.Value)
							.SumAsync(p => p.Amount);
						fabric.Balance = fabric.Amount - totalPaid;
						_context.Fabrics.Update(fabric);
						await _context.SaveChangesAsync();
					}
				}

				await _balanceService.UpdatePartyBalance(partyId);

				TempData["Success"] = "Payment deleted successfully!";
			}

			return RedirectToAction(nameof(Payments));
		}

		// Helper methods
		private async Task<List<SelectListItem>> GetSuppliers()
		{
			var suppliers = await _context.Parties
				.Where(p => p.PartyType == "Supplier" && p.IsActive)
				.ToListAsync();

			var result = new List<SelectListItem>();
			foreach (var sup in suppliers)
			{
				var totalFabric = await _context.Fabrics
					.Where(f => f.PartyId == sup.PartyId)
					.SumAsync(f => (decimal?)f.Amount) ?? 0;

				var totalPaid = await _context.FabricPayments
					.Where(fp => fp.PartyId == sup.PartyId)
					.SumAsync(fp => (decimal?)fp.Amount) ?? 0;

				var balance = totalFabric - totalPaid;

				result.Add(new SelectListItem
				{
					Value = sup.PartyId.ToString(),
					Text = $"{sup.PartyName} (Balance: ₨ {balance:N0})"
				});
			}

			return result;
		}

		private List<SelectListItem> GetPaymentMethods()
		{
			return new List<SelectListItem>
			{
				new SelectListItem { Value = "Cash", Text = "Cash" },
				new SelectListItem { Value = "Bank", Text = "Bank Transfer" },
				new SelectListItem { Value = "EasyPaisa", Text = "EasyPaisa" },
				new SelectListItem { Value = "JazzCash", Text = "JazzCash" }
			};
		}

		private bool FabricExists(int id)
		{
			return _context.Fabrics.Any(e => e.Id == id);
		}
	}
}