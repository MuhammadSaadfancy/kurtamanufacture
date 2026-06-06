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
	public class ReceiverController : Controller
	{
		private readonly ApplicationDbContext _context;
		private readonly IBalanceService _balanceService;
		private readonly ISmsService _smsService;

		public ReceiverController(ApplicationDbContext context, IBalanceService balanceService, ISmsService smsService)
		{
			_context = context;
			_balanceService = balanceService;
			_smsService = smsService;
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

		// GET: Receiver List
		public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate, int? partyId, int page = 1)
		{
			if (!IsLoggedIn()) return RedirectToAction("Login", "Auth");

			ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd") ?? DateTime.Now.AddDays(-30).ToString("yyyy-MM-dd");
			ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd") ?? DateTime.Now.ToString("yyyy-MM-dd");
			ViewBag.SelectedParty = partyId;

			var customers = await _context.Parties
				.Where(p => p.PartyType == "Customer" && p.IsActive)
				.Select(p => new SelectListItem { Value = p.PartyId.ToString(), Text = p.PartyName ?? "" })
				.ToListAsync();
			customers.Insert(0, new SelectListItem { Value = "", Text = "-- All Customers --" });
			ViewBag.Customers = customers;

			var query = _context.Receivers
				.Include(r => r.Party)
				.Include(r => r.Sender)
				.AsQueryable();

			if (fromDate.HasValue)
				query = query.Where(r => r.Date.Date >= fromDate.Value.Date);

			if (toDate.HasValue)
				query = query.Where(r => r.Date.Date <= toDate.Value.Date);

			if (partyId.HasValue && partyId > 0)
				query = query.Where(r => r.PartyId == partyId.Value);

			int pageSize = 15;
			int totalRecords = await query.CountAsync();
			int totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

			var receivers = await query
				.OrderByDescending(r => r.Date)
				.Skip((page - 1) * pageSize)
				.Take(pageSize)
				.Select(r => new ReceiverListVM
				{
					Id = r.Id,
					Date = r.Date,
					PartyName = r.Party.PartyName ?? "",
					Amount = r.Amount,
					PaymentMethod = r.PaymentMethod ?? "Cash",
					BillReference = $"Bill #{r.SenderId}"
				})
				.ToListAsync();

			ViewBag.CurrentPage = page;
			ViewBag.TotalPages = totalPages;
			ViewBag.TotalRecords = totalRecords;
			ViewBag.TotalAmount = await query.SumAsync(r => (decimal?)r.Amount) ?? 0;

			return View(receivers);
		}

		// GET: Create Receiver (Receive Payment)
		public async Task<IActionResult> Create(int? partyId, int? senderId)
		{
			if (!IsLoggedIn()) return RedirectToAction("Login", "Auth");

			var model = new ReceiverVM
			{
				Date = DateTime.Now,
				PaymentMethods = GetPaymentMethods(),
				Customers = await GetCustomers()
			};

			if (senderId.HasValue)
			{
				var sender = await _context.Senders
					.Include(s => s.Party)
					.FirstOrDefaultAsync(s => s.Id == senderId);

				if (sender != null)
				{
					model.SenderId = sender.Id;
					model.PartyId = sender.PartyId;
					model.PartyName = sender.Party?.PartyName ?? "";
					model.BillReference = $"Bill #{sender.Id} - Balance: ₨ {sender.Balance:N0}";
					model.Amount = sender.Balance;
					model.PendingBills = await GetPendingBillsSelectList(sender.PartyId);
				}
			}
			else if (partyId.HasValue)
			{
				model.PartyId = partyId.Value;
				var party = await _context.Parties.FindAsync(partyId.Value);
				model.PartyName = party?.PartyName ?? "";
				model.PendingBills = await GetPendingBillsSelectList(partyId.Value);
			}

			return View(model);
		}

		// POST: Create Receiver - WITH SMS
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(ReceiverVM model)
		{
			if (!IsLoggedIn()) return RedirectToAction("Login", "Auth");

			try
			{
				var sender = await _context.Senders.FindAsync(model.SenderId);
				if (sender == null)
				{
					TempData["Error"] = "Bill not found!";
					model.PaymentMethods = GetPaymentMethods();
					model.Customers = await GetCustomers();
					model.PendingBills = await GetPendingBillsSelectList(model.PartyId);
					return View(model);
				}

				if (model.Amount <= 0)
				{
					TempData["Error"] = "Payment amount must be greater than 0!";
					model.PaymentMethods = GetPaymentMethods();
					model.Customers = await GetCustomers();
					model.PendingBills = await GetPendingBillsSelectList(model.PartyId);
					return View(model);
				}

				if (model.Amount > sender.Balance)
				{
					TempData["Error"] = $"Payment amount ₨ {model.Amount:N0} exceeds pending balance ₨ {sender.Balance:N0}!";
					model.PaymentMethods = GetPaymentMethods();
					model.Customers = await GetCustomers();
					model.PendingBills = await GetPendingBillsSelectList(model.PartyId);
					return View(model);
				}

				var receiver = new Receiver
				{
					Date = model.Date,
					PartyId = model.PartyId,
					SenderId = model.SenderId,
					Amount = model.Amount,
					PaymentMethod = model.PaymentMethod ?? "Cash",
					Notes = model.Notes ?? "",
					CreatedBy = GetCurrentUserId(),
					CreatedAt = DateTime.Now
				};

				_context.Receivers.Add(receiver);
				await _context.SaveChangesAsync();

				// Manually update sender balance
				var totalReceived = await _context.Receivers
					.Where(r => r.SenderId == model.SenderId)
					.SumAsync(r => r.Amount);

				sender.Balance = sender.TotalAmount - totalReceived;
				_context.Senders.Update(sender);
				await _context.SaveChangesAsync();

				await _balanceService.UpdatePartyBalance(model.PartyId);

				// 🔥 ========== SMS CODE - YAHAN PASTE KARO ========== 🔥
				try
				{
					var party = await _context.Parties.FindAsync(model.PartyId);
					if (party != null && !string.IsNullOrEmpty(party.Phone))
					{
						var updatedSender = await _context.Senders.FindAsync(model.SenderId);
						await _smsService.SendPaymentReceivedSms(
							party.Phone,
							party.PartyName,
							model.Amount,
							updatedSender?.Balance ?? 0
						);
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine($"SMS failed: {ex.Message}");
				}
				// 🔥 ========== SMS CODE END ========== 🔥

				TempData["Success"] = $"✅ Payment of ₨ {model.Amount:N0} received! New balance: ₨ {sender.Balance:N0}";
				return RedirectToAction(nameof(Index));
			}
			catch (Exception ex)
			{
				TempData["Error"] = $"Save failed: {ex.Message}";
				Console.WriteLine($"Error: {ex.Message}");
				model.PaymentMethods = GetPaymentMethods();
				model.Customers = await GetCustomers();
				model.PendingBills = await GetPendingBillsSelectList(model.PartyId);
				return View(model);
			}
		}

		// GET: Delete Payment
		public async Task<IActionResult> Delete(int id)
		{
			if (!IsLoggedIn()) return RedirectToAction("Login", "Auth");

			var receiver = await _context.Receivers
				.Include(r => r.Party)
				.Include(r => r.Sender)
				.FirstOrDefaultAsync(r => r.Id == id);

			if (receiver == null)
			{
				TempData["Error"] = "Payment record not found!";
				return RedirectToAction(nameof(Index));
			}

			return View(receiver);
		}

		// POST: Delete Payment
		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed(int id)
		{
			if (!IsLoggedIn()) return RedirectToAction("Login", "Auth");

			var receiver = await _context.Receivers.FindAsync(id);
			if (receiver != null)
			{
				var senderId = receiver.SenderId;
				var partyId = receiver.PartyId;

				_context.Receivers.Remove(receiver);
				await _context.SaveChangesAsync();

				// Update sender balance after deletion
				var sender = await _context.Senders.FindAsync(senderId);
				if (sender != null)
				{
					var totalReceived = await _context.Receivers
						.Where(r => r.SenderId == senderId)
						.SumAsync(r => r.Amount);

					sender.Balance = sender.TotalAmount - totalReceived;
					_context.Senders.Update(sender);
					await _context.SaveChangesAsync();
				}

				await _balanceService.UpdatePartyBalance(partyId);

				TempData["Success"] = "Payment record deleted successfully!";
			}

			return RedirectToAction(nameof(Index));
		}

		// AJAX: Get Pending Bills for Customer
		[HttpGet]
		public async Task<IActionResult> GetPendingBills(int customerId)
		{
			var bills = await _context.Senders
				.Where(s => s.PartyId == customerId && s.Balance > 0)
				.OrderByDescending(s => s.Date)
				.Select(s => new
				{
					id = s.Id,
					display = $"Bill #{s.Id} - {s.Date:dd-MMM-yyyy} - Pending: ₨ {s.Balance:N0}"
				})
				.ToListAsync();

			return Json(bills);
		}

		// Helper Methods
		private async Task<List<SelectListItem>> GetCustomers()
		{
			return await _context.Parties
				.Where(p => p.PartyType == "Customer" && p.IsActive)
				.Select(p => new SelectListItem
				{
					Value = p.PartyId.ToString(),
					Text = $"{p.PartyName ?? ""} (Balance: ₨ {p.CurrentBalance:N0})"
				})
				.ToListAsync();
		}

		private async Task<List<SelectListItem>> GetPendingBillsSelectList(int partyId)
		{
			return await _context.Senders
				.Where(s => s.PartyId == partyId && s.Balance > 0)
				.OrderByDescending(s => s.Date)
				.Select(s => new SelectListItem
				{
					Value = s.Id.ToString(),
					Text = $"Bill #{s.Id} - {s.Date:dd-MMM-yyyy} - Pending: ₨ {s.Balance:N0}"
				})
				.ToListAsync();
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
	}
}