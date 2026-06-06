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
	public class ProductionController : Controller
	{
		private readonly ApplicationDbContext _context;
		private readonly IBalanceService _balanceService;
		private readonly IFileUploadService _fileUploadService;
		private readonly ISmsService _smsService;  // 🔥 ADDED

		public ProductionController(ApplicationDbContext context, IBalanceService balanceService, IFileUploadService fileUploadService, ISmsService smsService)  // 🔥 ADDED smsService
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

		// GET: Production Dashboard
		public async Task<IActionResult> Dashboard()
		{
			if (!IsLoggedIn()) return RedirectToAction("Login", "Auth");

			var dashboard = new ProductionDashboardVM
			{
				TotalDesignAmount = await _context.ProductionTransactions
					.Where(p => p.ModuleType == "Design").SumAsync(p => (decimal?)p.Amount) ?? 0,
				TotalCuttingAmount = await _context.ProductionTransactions
					.Where(p => p.ModuleType == "Cutting").SumAsync(p => (decimal?)p.Amount) ?? 0,
				TotalCMTAmount = await _context.ProductionTransactions
					.Where(p => p.ModuleType == "CMT").SumAsync(p => (decimal?)p.Amount) ?? 0,
				TotalButtonAmount = await _context.ProductionTransactions
					.Where(p => p.ModuleType == "KurtaButton").SumAsync(p => (decimal?)p.Amount) ?? 0,
				TotalEndingAmount = await _context.ProductionTransactions
					.Where(p => p.ModuleType == "EndingWork").SumAsync(p => (decimal?)p.Amount) ?? 0,
				TotalPaid = await _context.ProductionPayments.SumAsync(p => (decimal?)p.Amount) ?? 0,
				WorkerBalances = await GetWorkerBalances()
			};

			dashboard.TotalBalance = (dashboard.TotalDesignAmount + dashboard.TotalCuttingAmount +
									   dashboard.TotalCMTAmount + dashboard.TotalButtonAmount +
									   dashboard.TotalEndingAmount) - dashboard.TotalPaid;

			return View(dashboard);
		}

		// GET: Production List (Filter by Module)
		public async Task<IActionResult> Index(string moduleType, string subType, int? partyId, DateTime? fromDate, DateTime? toDate, int page = 1)
		{
			if (!IsLoggedIn()) return RedirectToAction("Login", "Auth");

			ViewBag.ModuleType = moduleType;
			ViewBag.SubType = subType;
			ViewBag.SelectedParty = partyId;
			ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
			ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
			ViewBag.ActiveTab = moduleType;

			var query = _context.ProductionTransactions
				.Include(p => p.Party)
				.AsQueryable();

			if (!string.IsNullOrEmpty(moduleType))
				query = query.Where(p => p.ModuleType == moduleType);

			if (!string.IsNullOrEmpty(subType))
				query = query.Where(p => p.SubType == subType);

			if (partyId.HasValue)
				query = query.Where(p => p.PartyId == partyId);

			if (fromDate.HasValue)
				query = query.Where(p => p.Date.Date >= fromDate.Value.Date);

			if (toDate.HasValue)
				query = query.Where(p => p.Date.Date <= toDate.Value.Date);

			int pageSize = 15;
			int totalRecords = await query.CountAsync();
			int totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

			var productions = await query
				.OrderByDescending(p => p.Date)
				.Skip((page - 1) * pageSize)
				.Take(pageSize)
				.Select(p => new ProductionListVM
				{
					Id = p.Id,
					Date = p.Date,
					ModuleType = p.ModuleType ?? "",
					SubType = p.SubType ?? "",
					PartyName = p.Party != null ? p.Party.PartyName ?? "" : "",
					ItemInfo = p.ItemName ?? (p.Dozen.HasValue ? $"{p.Dozen} Dozen" : (p.Pieces.HasValue ? $"{p.Pieces} Pieces" : "-")),
					Amount = p.Amount,
					Balance = p.Balance,
					Status = p.Status ?? "Pending"
				})
				.ToListAsync();

			ViewBag.CurrentPage = page;
			ViewBag.TotalPages = totalPages;
			ViewBag.TotalRecords = totalRecords;
			ViewBag.TotalAmount = await query.SumAsync(p => (decimal?)p.Amount) ?? 0;
			ViewBag.TotalBalance = await query.SumAsync(p => (decimal?)p.Balance) ?? 0;

			ViewBag.Workers = await GetWorkersByType(moduleType);

			return View("Index", productions);
		}

		// GET: Design (MRID & PRINT)
		public async Task<IActionResult> Design(string subType, int page = 1)
		{
			if (!IsLoggedIn()) return RedirectToAction("Login", "Auth");
			ViewBag.ActiveTab = "Design";
			ViewBag.SubType = subType;
			return await Index("Design", subType, null, null, null, page);
		}

		// GET: Cutting
		public async Task<IActionResult> Cutting(int page = 1)
		{
			if (!IsLoggedIn()) return RedirectToAction("Login", "Auth");
			ViewBag.ActiveTab = "Cutting";
			return await Index("Cutting", null, null, null, null, page);
		}

		// GET: CMT (Shalwar/Kameez/Koti)
		public async Task<IActionResult> CMT(string subType, int page = 1)
		{
			if (!IsLoggedIn()) return RedirectToAction("Login", "Auth");
			ViewBag.ActiveTab = "CMT";
			ViewBag.SubType = subType;
			return await Index("CMT", subType, null, null, null, page);
		}

		// GET: KurtaButton
		public async Task<IActionResult> KurtaButton(int page = 1)
		{
			if (!IsLoggedIn()) return RedirectToAction("Login", "Auth");
			ViewBag.ActiveTab = "KurtaButton";
			return await Index("KurtaButton", null, null, null, null, page);
		}

		// GET: EndingWork (Cropping/Press/Packing)
		public async Task<IActionResult> EndingWork(string subType, int page = 1)
		{
			if (!IsLoggedIn()) return RedirectToAction("Login", "Auth");
			ViewBag.ActiveTab = "EndingWork";
			ViewBag.SubType = subType;
			return await Index("EndingWork", subType, null, null, null, page);
		}

		// GET: Create Production
		public async Task<IActionResult> Create(string moduleType, string subType)
		{
			if (!IsLoggedIn()) return RedirectToAction("Login", "Auth");

			var model = new ProductionVM
			{
				ModuleType = moduleType,
				SubType = subType,
				Date = DateTime.Now,
				Workers = await GetWorkersByType(moduleType),
				SubTypes = GetSubTypes(moduleType)
			};

			ViewBag.ActiveTab = moduleType;
			ViewBag.ModuleDisplayName = GetModuleDisplayName(moduleType, subType);
			return View(model);
		}

		// POST: Create Production
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(ProductionVM model)
		{
			if (!IsLoggedIn())
			{
				return RedirectToAction("Login", "Auth");
			}

			try
			{
				var transaction = new ProductionTransaction
				{
					Date = model.Date != default ? model.Date : DateTime.Now,
					ModuleType = string.IsNullOrWhiteSpace(model.ModuleType) ? "Unknown" : model.ModuleType.Trim(),
					SubType = string.IsNullOrWhiteSpace(model.SubType) ? "" : model.SubType.Trim(),
					PartyId = model.PartyId > 0 ? model.PartyId : 1,
					ItemName = string.IsNullOrWhiteSpace(model.ItemName) ? "No Item" : model.ItemName.Trim(),
					Dozen = model.Dozen > 0 ? model.Dozen : 0,
					Pieces = model.Pieces > 0 ? model.Pieces : 0,
					Amount = model.Amount > 0 ? model.Amount : 0,
					Balance = model.Amount > 0 ? model.Amount : 0,
					Status = "Pending",
					Notes = model.Notes?.Trim() ?? "",
					CreatedBy = GetCurrentUserId(),
					CreatedAt = DateTime.Now
				};

				_context.ProductionTransactions.Add(transaction);
				await _context.SaveChangesAsync();

				if (_balanceService != null && transaction.PartyId.HasValue)
				{
					await _balanceService.UpdatePartyBalance(transaction.PartyId.Value);
				}

				TempData["Success"] = $"✅ {GetModuleDisplayName(model.ModuleType, model.SubType)} saved successfully! ID: {transaction.Id}";
				return RedirectToAction(GetRedirectAction(model.ModuleType), new { subType = model.ModuleType == "Design" || model.ModuleType == "CMT" || model.ModuleType == "EndingWork" ? model.SubType : null });
			}
			catch (Exception ex)
			{
				TempData["Error"] = $"Save failed: {ex.Message}";
				model.Workers = await GetWorkersByType(model.ModuleType);
				model.SubTypes = GetSubTypes(model.ModuleType);
				ViewBag.ActiveTab = model.ModuleType;
				ViewBag.ModuleDisplayName = GetModuleDisplayName(model.ModuleType, model.SubType);
				return View(model);
			}
		}

		// GET: Edit Production
		public async Task<IActionResult> Edit(int id)
		{
			if (!IsLoggedIn()) return RedirectToAction("Login", "Auth");

			var transaction = await _context.ProductionTransactions.FindAsync(id);
			if (transaction == null)
			{
				TempData["Error"] = "Record not found!";
				return RedirectToAction("Index");
			}

			var model = new ProductionVM
			{
				Id = transaction.Id,
				Date = transaction.Date,
				ModuleType = transaction.ModuleType ?? "",
				SubType = transaction.SubType ?? "",
				PartyId = transaction.PartyId,
				ItemName = transaction.ItemName ?? "",
				Dozen = transaction.Dozen,
				Pieces = transaction.Pieces,
				Amount = transaction.Amount,
				Balance = transaction.Balance,
				Notes = transaction.Notes ?? "",
				Workers = await GetWorkersByType(transaction.ModuleType ?? ""),
				SubTypes = GetSubTypes(transaction.ModuleType ?? "")
			};

			ViewBag.ActiveTab = transaction.ModuleType;
			ViewBag.ModuleDisplayName = GetModuleDisplayName(transaction.ModuleType ?? "", transaction.SubType ?? "");
			return View(model);
		}

		// POST: Edit Production
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(int id, ProductionVM model)
		{
			if (!IsLoggedIn()) return RedirectToAction("Login", "Auth");

			if (id != model.Id) return NotFound();

			try
			{
				var transaction = await _context.ProductionTransactions.FindAsync(id);
				if (transaction == null)
				{
					TempData["Error"] = "Record not found!";
					return RedirectToAction("Index");
				}

				var oldPartyId = transaction.PartyId;

				transaction.Date = model.Date != default ? model.Date : DateTime.Now;
				transaction.PartyId = model.PartyId > 0 ? model.PartyId : 1;
				transaction.ItemName = string.IsNullOrWhiteSpace(model.ItemName) ? "No Item" : model.ItemName.Trim();
				transaction.Dozen = model.Dozen > 0 ? model.Dozen : 0;
				transaction.Pieces = model.Pieces > 0 ? model.Pieces : 0;
				transaction.Amount = model.Amount > 0 ? model.Amount : 0;
				transaction.Notes = model.Notes?.Trim() ?? "";

				var totalPaid = await _context.ProductionPayments
					.Where(p => p.TransactionId == id)
					.SumAsync(p => (decimal?)p.Amount) ?? 0;
				transaction.Balance = model.Amount - totalPaid;
				transaction.Status = transaction.Balance == 0 ? "Paid" : (transaction.Balance < model.Amount ? "Partial" : "Pending");

				_context.Update(transaction);
				await _context.SaveChangesAsync();

				if (_balanceService != null)
				{
					if (oldPartyId != transaction.PartyId)
					{
						if (oldPartyId.HasValue) await _balanceService.UpdatePartyBalance(oldPartyId.Value);
						if (transaction.PartyId.HasValue) await _balanceService.UpdatePartyBalance(transaction.PartyId.Value);
					}
					else if (transaction.PartyId.HasValue)
					{
						await _balanceService.UpdatePartyBalance(transaction.PartyId.Value);
					}
				}

				TempData["Success"] = "Record updated successfully!";
				return RedirectToAction(GetRedirectAction(transaction.ModuleType ?? ""), new { subType = transaction.ModuleType == "Design" || transaction.ModuleType == "CMT" || transaction.ModuleType == "EndingWork" ? transaction.SubType : null });
			}
			catch (Exception ex)
			{
				TempData["Error"] = $"Update failed: {ex.Message}";
				model.Workers = await GetWorkersByType(model.ModuleType);
				model.SubTypes = GetSubTypes(model.ModuleType);
				ViewBag.ActiveTab = model.ModuleType;
				ViewBag.ModuleDisplayName = GetModuleDisplayName(model.ModuleType, model.SubType);
				return View(model);
			}
		}

		// GET: Delete Production
		public async Task<IActionResult> Delete(int id)
		{
			if (!IsLoggedIn()) return RedirectToAction("Login", "Auth");

			var transaction = await _context.ProductionTransactions
				.Include(p => p.Party)
				.Include(p => p.ProductionPayments)
				.FirstOrDefaultAsync(p => p.Id == id);

			if (transaction == null)
			{
				TempData["Error"] = "Record not found!";
				return RedirectToAction("Index");
			}

			ViewBag.HasPayments = transaction.ProductionPayments.Any();
			ViewBag.PaymentCount = transaction.ProductionPayments.Count();
			ViewBag.TotalPaid = transaction.ProductionPayments.Sum(p => p.Amount);
			ViewBag.ActiveTab = transaction.ModuleType;

			return View(transaction);
		}

		// POST: Delete Production
		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed(int id)
		{
			if (!IsLoggedIn()) return RedirectToAction("Login", "Auth");

			var transaction = await _context.ProductionTransactions
				.Include(p => p.ProductionPayments)
				.FirstOrDefaultAsync(p => p.Id == id);

			if (transaction != null)
			{
				var partyId = transaction.PartyId;

				if (transaction.ProductionPayments.Any())
					_context.ProductionPayments.RemoveRange(transaction.ProductionPayments);

				_context.ProductionTransactions.Remove(transaction);
				await _context.SaveChangesAsync();

				if (partyId.HasValue)
					await _balanceService.UpdatePartyBalance(partyId.Value);

				TempData["Success"] = "Record deleted successfully!";
			}

			return RedirectToAction("Index");
		}

		// GET: Get Pending Transactions (AJAX)
		[HttpGet]
		public async Task<IActionResult> GetPendingTransactions(int partyId)
		{
			var transactions = await _context.ProductionTransactions
				.Where(t => t.PartyId == partyId && t.Balance > 0)
				.Select(t => new
				{
					value = t.Id,
					text = $"#{t.Id} - {t.ModuleType ?? "Unknown"} - Balance: ₨ {t.Balance:N0}"
				})
				.ToListAsync();

			return Json(transactions);
		}

		// GET: Payments List
		public async Task<IActionResult> Payments(DateTime? fromDate, DateTime? toDate, int? partyId, int page = 1)
		{
			if (!IsLoggedIn()) return RedirectToAction("Login", "Auth");

			ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
			ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
			ViewBag.SelectedParty = partyId;

			var query = _context.ProductionPayments
				.Include(p => p.Party)
				.Include(p => p.ProductionTransaction)
				.AsQueryable();

			if (fromDate.HasValue)
				query = query.Where(p => p.Date.Date >= fromDate.Value.Date);

			if (toDate.HasValue)
				query = query.Where(p => p.Date.Date <= toDate.Value.Date);

			if (partyId.HasValue)
				query = query.Where(p => p.PartyId == partyId);

			int pageSize = 15;
			int totalRecords = await query.CountAsync();
			int totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
			decimal totalAmount = await query.SumAsync(p => (decimal?)p.Amount) ?? 0;

			var payments = await query
				.OrderByDescending(p => p.Date)
				.Skip((page - 1) * pageSize)
				.Take(pageSize)
				.Select(p => new ProductionPaymentListVM
				{
					Id = p.Id,
					Date = p.Date,
					PartyName = p.Party != null ? p.Party.PartyName ?? "" : "",
					ModuleType = p.ProductionTransaction != null ? p.ProductionTransaction.ModuleType ?? "" : "",
					Amount = p.Amount,
					PaymentMethod = p.PaymentMethod ?? "Cash",
					PaymentImagePath = p.PaymentImagePath ?? ""
				})
				.ToListAsync();

			ViewBag.CurrentPage = page;
			ViewBag.TotalPages = totalPages;
			ViewBag.TotalRecords = totalRecords;
			ViewBag.TotalAmount = totalAmount;

			ViewBag.Workers = await _context.Parties
				.Where(p => (p.PartyType == "CMTWorker" || p.PartyType == "CuttingMaster" ||
						   p.PartyType == "Designer" || p.PartyType == "ButtonWorker" ||
						   p.PartyType == "PressWorker") && p.IsActive)
				.Select(p => new SelectListItem { Value = p.PartyId.ToString(), Text = p.PartyName ?? "" })
				.ToListAsync();
			ViewBag.Workers.Insert(0, new SelectListItem { Value = "", Text = "-- All Workers --" });

			return View(payments);
		}

		// GET: Create Payment
		public async Task<IActionResult> CreatePayment(int? transactionId, int? partyId)
		{
			if (!IsLoggedIn()) return RedirectToAction("Login", "Auth");

			var workers = await GetAllWorkers();

			var model = new ProductionPaymentVM
			{
				Date = DateTime.Now,
				PaymentMethods = GetPaymentMethods(),
				Workers = workers,
				Transactions = new List<SelectListItem>()
			};

			if (transactionId.HasValue)
			{
				var transaction = await _context.ProductionTransactions
					.Include(t => t.Party)
					.FirstOrDefaultAsync(t => t.Id == transactionId);

				if (transaction != null)
				{
					model.TransactionId = transaction.Id;
					model.PartyId = transaction.PartyId ?? 0;
					model.PartyName = transaction.Party?.PartyName ?? "";
					model.TransactionReference = $"{transaction.ModuleType} - Balance: ₨ {transaction.Balance:N0}";
					model.Amount = transaction.Balance;

					model.Transactions = await _context.ProductionTransactions
						.Where(t => t.PartyId == transaction.PartyId && t.Balance > 0)
						.Select(t => new SelectListItem
						{
							Value = t.Id.ToString(),
							Text = $"#{t.Id} - {t.ModuleType ?? "Unknown"} - Balance: ₨ {t.Balance:N0}"
						})
						.ToListAsync();
				}
			}

			if (partyId.HasValue && !transactionId.HasValue)
			{
				model.PartyId = partyId.Value;
				var party = await _context.Parties.FindAsync(partyId.Value);
				model.PartyName = party?.PartyName ?? "";

				model.Transactions = await _context.ProductionTransactions
					.Where(t => t.PartyId == partyId.Value && t.Balance > 0)
					.Select(t => new SelectListItem
					{
						Value = t.Id.ToString(),
						Text = $"#{t.Id} - {t.ModuleType ?? "Unknown"} - Balance: ₨ {t.Balance:N0}"
					})
					.ToListAsync();
			}

			return View(model);
		}

		// POST: Create Payment - 🔥 WITH SMS
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> CreatePayment(ProductionPaymentVM model)
		{
			if (!IsLoggedIn()) return RedirectToAction("Login", "Auth");

			try
			{
				var transaction = await _context.ProductionTransactions.FindAsync(model.TransactionId);
				if (transaction == null)
				{
					TempData["Error"] = "Transaction not found!";
					model.PaymentMethods = GetPaymentMethods();
					model.Workers = await GetAllWorkers();
					model.Transactions = await GetPendingTransactionsList(model.PartyId);
					return View(model);
				}

				if (model.Amount <= 0)
				{
					TempData["Error"] = "Payment amount must be greater than 0!";
					model.PaymentMethods = GetPaymentMethods();
					model.Workers = await GetAllWorkers();
					return View(model);
				}

				if (model.Amount > transaction.Balance)
				{
					TempData["Error"] = $"Payment amount ₨ {model.Amount:N0} exceeds pending balance ₨ {transaction.Balance:N0}!";
					model.PaymentMethods = GetPaymentMethods();
					model.Workers = await GetAllWorkers();
					model.Transactions = await GetPendingTransactionsList(model.PartyId);
					return View(model);
				}

				string imagePath = "";
				if (model.PaymentImage != null && model.PaymentImage.Length > 0)
				{
					imagePath = await _fileUploadService.UploadPaymentImage(model.PaymentImage, "production_payment");
				}

				var payment = new ProductionPayment
				{
					Date = model.Date,
					PartyId = model.PartyId,
					TransactionId = model.TransactionId,
					Amount = model.Amount,
					PaymentMethod = model.PaymentMethod ?? "Cash",
					Notes = model.Notes ?? "",
					PaymentImagePath = imagePath,
					CreatedBy = GetCurrentUserId(),
					CreatedAt = DateTime.Now
				};

				_context.ProductionPayments.Add(payment);
				await _context.SaveChangesAsync();

				// Update transaction balance
				var totalPaid = await _context.ProductionPayments
					.Where(p => p.TransactionId == model.TransactionId)
					.SumAsync(p => p.Amount);
				transaction.Balance = transaction.Amount - totalPaid;
				transaction.Status = transaction.Balance == 0 ? "Paid" : (transaction.Balance < transaction.Amount ? "Partial" : "Pending");
				_context.ProductionTransactions.Update(transaction);
				await _context.SaveChangesAsync();

				await _balanceService.UpdatePartyBalance(model.PartyId);

				// 🔥 ========== SMS CODE - YAHAN ADD KIYA ========== 🔥
				try
				{
					var party = await _context.Parties.FindAsync(model.PartyId);
					if (party != null && !string.IsNullOrEmpty(party.Phone))
					{
						await _smsService.SendPaymentSentSms(
							party.Phone,
							party.PartyName,
							model.Amount,
							transaction.Balance,
							transaction.ModuleType ?? "Production"
						);
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine($"SMS failed: {ex.Message}");
				}
				// 🔥 ========== SMS CODE END ========== 🔥

				TempData["Success"] = $"✅ Payment of ₨ {model.Amount:N0} recorded successfully! New balance: ₨ {transaction.Balance:N0}";
				return RedirectToAction(nameof(Payments));
			}
			catch (Exception ex)
			{
				TempData["Error"] = $"Save failed: {ex.Message}";
				model.PaymentMethods = GetPaymentMethods();
				model.Workers = await GetAllWorkers();
				model.Transactions = await GetPendingTransactionsList(model.PartyId);
				return View(model);
			}
		}

		// GET: Delete Payment
		public async Task<IActionResult> DeletePayment(int id)
		{
			if (!IsLoggedIn()) return RedirectToAction("Login", "Auth");

			var payment = await _context.ProductionPayments
				.Include(p => p.Party)
				.Include(p => p.ProductionTransaction)
				.FirstOrDefaultAsync(p => p.Id == id);

			if (payment == null)
			{
				TempData["Error"] = "Payment not found!";
				return RedirectToAction(nameof(Payments));
			}

			return View(payment);
		}

		// POST: Delete Payment
		[HttpPost, ActionName("DeletePayment")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeletePaymentConfirmed(int id)
		{
			if (!IsLoggedIn()) return RedirectToAction("Login", "Auth");

			var payment = await _context.ProductionPayments.FindAsync(id);
			if (payment != null)
			{
				var transactionId = payment.TransactionId;
				var partyId = payment.PartyId;

				if (!string.IsNullOrEmpty(payment.PaymentImagePath))
				{
					_fileUploadService.DeletePaymentImage(payment.PaymentImagePath);
				}

				_context.ProductionPayments.Remove(payment);
				await _context.SaveChangesAsync();

				// Update transaction balance
				var transaction = await _context.ProductionTransactions.FindAsync(transactionId);
				if (transaction != null)
				{
					var totalPaid = await _context.ProductionPayments
						.Where(p => p.TransactionId == transactionId)
						.SumAsync(p => p.Amount);
					transaction.Balance = transaction.Amount - totalPaid;
					transaction.Status = transaction.Balance == 0 ? "Paid" : (transaction.Balance < transaction.Amount ? "Partial" : "Pending");
					_context.ProductionTransactions.Update(transaction);
					await _context.SaveChangesAsync();
				}

				await _balanceService.UpdatePartyBalance(partyId);

				TempData["Success"] = "Payment deleted successfully!";
			}

			return RedirectToAction(nameof(Payments));
		}

		// Helper Methods
		private async Task<List<SelectListItem>> GetAllWorkers()
		{
			var workers = await _context.Parties
				.Where(p => (p.PartyType == "CMTWorker" || p.PartyType == "CuttingMaster" ||
							p.PartyType == "Designer" || p.PartyType == "ButtonWorker" ||
							p.PartyType == "PressWorker") && p.IsActive)
				.Select(p => new SelectListItem
				{
					Value = p.PartyId.ToString(),
					Text = $"{p.PartyName ?? "Unknown"} - {p.PartyType ?? "Worker"}"
				})
				.ToListAsync();

			return workers;
		}

		private async Task<List<SelectListItem>> GetWorkersByType(string moduleType)
		{
			string partyType = moduleType switch
			{
				"Design" => "Designer",
				"Cutting" => "CuttingMaster",
				"CMT" => "CMTWorker",
				"KurtaButton" => "ButtonWorker",
				"EndingWork" => "PressWorker",
				_ => null
			};

			if (string.IsNullOrEmpty(partyType))
				return new List<SelectListItem>();

			return await _context.Parties
				.Where(p => p.PartyType == partyType && p.IsActive)
				.Select(p => new SelectListItem
				{
					Value = p.PartyId.ToString(),
					Text = $"{p.PartyName ?? "Unknown"} - {partyType}"
				})
				.ToListAsync();
		}

		private async Task<List<WorkerBalanceVM>> GetWorkerBalances()
		{
			var workers = await _context.Parties
				.Where(p => (p.PartyType == "CMTWorker" || p.PartyType == "CuttingMaster" ||
						   p.PartyType == "Designer" || p.PartyType == "ButtonWorker" ||
						   p.PartyType == "PressWorker") && p.IsActive)
				.ToListAsync();

			var balances = new List<WorkerBalanceVM>();
			foreach (var worker in workers)
			{
				var totalAmount = await _context.ProductionTransactions
					.Where(t => t.PartyId == worker.PartyId)
					.SumAsync(t => (decimal?)t.Amount) ?? 0;

				var totalPaid = await _context.ProductionPayments
					.Where(p => p.PartyId == worker.PartyId)
					.SumAsync(p => (decimal?)p.Amount) ?? 0;

				balances.Add(new WorkerBalanceVM
				{
					PartyId = worker.PartyId,
					PartyName = worker.PartyName ?? "",
					WorkerType = worker.PartyType ?? "",
					TotalAmount = totalAmount,
					TotalPaid = totalPaid,
					Balance = totalAmount - totalPaid
				});
			}

			return balances.OrderByDescending(b => b.Balance).ToList();
		}

		private List<SelectListItem> GetSubTypes(string moduleType)
		{
			return moduleType switch
			{
				"Design" => new List<SelectListItem>
				{
					new SelectListItem { Value = "MRID", Text = "MRID" },
					new SelectListItem { Value = "PRINT", Text = "PRINT" }
				},
				"CMT" => new List<SelectListItem>
				{
					new SelectListItem { Value = "Shalwar", Text = "Shalwar" },
					new SelectListItem { Value = "Kameez", Text = "Kameez" },
					new SelectListItem { Value = "Koti", Text = "Koti" }
				},
				"EndingWork" => new List<SelectListItem>
				{
					new SelectListItem { Value = "Cropping", Text = "Cropping" },
					new SelectListItem { Value = "Press", Text = "Press" },
					new SelectListItem { Value = "Packing", Text = "Packing" }
				},
				_ => new List<SelectListItem>()
			};
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

		private async Task<List<SelectListItem>> GetPendingTransactionsList(int? partyId)
		{
			var query = _context.ProductionTransactions
				.Where(t => t.Balance > 0);

			if (partyId.HasValue && partyId > 0)
				query = query.Where(t => t.PartyId == partyId);

			return await query
				.OrderByDescending(t => t.Date)
				.Select(t => new SelectListItem
				{
					Value = t.Id.ToString(),
					Text = $"#{t.Id} - {t.ModuleType ?? "Unknown"} - ₨ {t.Balance:N0} (Due)"
				})
				.ToListAsync();
		}

		private string GetModuleDisplayName(string moduleType, string subType)
		{
			return moduleType switch
			{
				"Design" => $"Design - {subType}",
				"Cutting" => "Cutting",
				"CMT" => $"CMT - {subType}",
				"KurtaButton" => "Kurta Buttons",
				"EndingWork" => $"Ending Work - {subType}",
				_ => moduleType ?? "Production"
			};
		}

		private string GetRedirectAction(string moduleType)
		{
			return moduleType switch
			{
				"Design" => "Design",
				"Cutting" => "Cutting",
				"CMT" => "CMT",
				"KurtaButton" => "KurtaButton",
				"EndingWork" => "EndingWork",
				_ => "Index"
			};
		}
	}
}