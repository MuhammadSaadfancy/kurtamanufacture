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
	public class SenderController : Controller
	{
		private readonly ApplicationDbContext _context;
		private readonly IBalanceService _balanceService;

		public SenderController(ApplicationDbContext context, IBalanceService balanceService)
		{
			_context = context;
			_balanceService = balanceService;
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

		// GET: Customer Dashboard
		public async Task<IActionResult> Dashboard()
		{
			if (!IsLoggedIn()) return RedirectToAction("Login", "Auth");

			var dashboard = new CustomerDashboardVM
			{
				TotalSales = await _context.Senders.SumAsync(s => (decimal?)s.TotalAmount) ?? 0,
				TotalReceived = await _context.Receivers.SumAsync(r => (decimal?)r.Amount) ?? 0,
				TotalCustomers = await _context.Parties.CountAsync(p => p.PartyType == "Customer" && p.IsActive),
				TotalBills = await _context.Senders.CountAsync(),
				CustomerBalances = await GetCustomerBalances()
			};

			dashboard.TotalPending = dashboard.TotalSales - dashboard.TotalReceived;

			return View(dashboard);
		}

		// GET: Sender List
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

			var query = _context.Senders.Include(s => s.Party).AsQueryable();

			if (fromDate.HasValue)
				query = query.Where(s => s.Date.Date >= fromDate.Value.Date);

			if (toDate.HasValue)
				query = query.Where(s => s.Date.Date <= toDate.Value.Date);

			if (partyId.HasValue && partyId > 0)
				query = query.Where(s => s.PartyId == partyId.Value);

			int pageSize = 15;
			int totalRecords = await query.CountAsync();
			int totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

			var senders = await query
				.OrderByDescending(s => s.Date)
				.Skip((page - 1) * pageSize)
				.Take(pageSize)
				.Select(s => new SenderListVM
				{
					Id = s.Id,
					Date = s.Date,
					PartyId = s.PartyId,
					PartyName = s.Party.PartyName ?? "",
					Quantity = s.Dozen.HasValue ? $"{s.Dozen} Dozen" : (s.Pieces.HasValue ? $"{s.Pieces} Pieces" : "-"),
					TotalAmount = s.TotalAmount,
					Balance = s.Balance,
					Status = s.Balance == 0 ? "Paid" : (s.Balance < s.TotalAmount ? "Partial" : "Pending"),
					TotalItems = s.TotalItems
				})
				.ToListAsync();

			ViewBag.CurrentPage = page;
			ViewBag.TotalPages = totalPages;
			ViewBag.TotalRecords = totalRecords;
			ViewBag.TotalAmount = await query.SumAsync(s => (decimal?)s.TotalAmount) ?? 0;
			ViewBag.TotalBalance = await query.SumAsync(s => (decimal?)s.Balance) ?? 0;

			return View(senders);
		}

		// GET: Create Sender (Single Item Bill)
		public async Task<IActionResult> Create()
		{
			if (!IsLoggedIn()) return RedirectToAction("Login", "Auth");

			var model = new SenderVM
			{
				Date = DateTime.Now,
				Customers = await GetCustomers()
			};
			return View(model);
		}

		// POST: Create Sender (Single Item Bill)
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(SenderVM model)
		{
			if (!IsLoggedIn()) return RedirectToAction("Login", "Auth");

			try
			{
				var sender = new Sender
				{
					Date = model.Date != default ? model.Date : DateTime.Now,
					PartyId = model.PartyId > 0 ? model.PartyId : 1,
					Dozen = model.Dozen > 0 ? model.Dozen : 0,
					Pieces = model.Pieces > 0 ? model.Pieces : 0,
					TotalAmount = model.TotalAmount > 0 ? model.TotalAmount : 0,
					Balance = model.TotalAmount > 0 ? model.TotalAmount : 0,
					TotalItems = 1,
					Notes = model.Notes?.Trim() ?? "",
					CreatedBy = GetCurrentUserId(),
					CreatedAt = DateTime.Now
				};

				_context.Senders.Add(sender);
				await _context.SaveChangesAsync();

				if (_balanceService != null)
				{
					await _balanceService.UpdateSenderBalance(sender.Id);
					await _balanceService.UpdatePartyBalance(model.PartyId);
				}

				TempData["Success"] = $"✅ Bill created successfully! ID: {sender.Id}, Amount: ₨ {model.TotalAmount:N0}";
				return RedirectToAction(nameof(Index));
			}
			catch (Exception ex)
			{
				TempData["Error"] = $"Save failed: {ex.Message}";
				model.Customers = await GetCustomers();
				return View(model);
			}
		}

		// GET: Create Multi-Item Bill
		public async Task<IActionResult> CreateMultiBill()
		{
			if (!IsLoggedIn()) return RedirectToAction("Login", "Auth");

			var model = new MultiItemBillVM
			{
				Date = DateTime.Now,
				Items = new List<BillItemVM> { new BillItemVM() },
				Customers = await GetCustomers()
			};

			return View(model);
		}

		// POST: Add Item Row (AJAX)
		[HttpPost]
		public IActionResult AddBillItem()
		{
			return PartialView("_BillItemRow", new BillItemVM());
		}

		// 🔥 FIXED: POST Create Multi-Item Bill
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> CreateMultiBill(MultiItemBillVM model)
		{
			if (!IsLoggedIn()) return RedirectToAction("Login", "Auth");

			// Get items from form directly - Count all items
			var itemNames = Request.Form["ItemName"].ToList();
			var dozens = Request.Form["Dozen"].Select(x => string.IsNullOrEmpty(x) ? 0 : decimal.Parse(x)).ToList();
			var piecess = Request.Form["Pieces"].Select(x => string.IsNullOrEmpty(x) ? 0 : decimal.Parse(x)).ToList();
			var prices = Request.Form["Price"].Select(x => string.IsNullOrEmpty(x) ? 0 : decimal.Parse(x)).ToList();
			var totals = Request.Form["TotalAmount"].Select(x => string.IsNullOrEmpty(x) ? 0 : decimal.Parse(x)).ToList();

			// 🔥 Log the count for debugging
			Console.WriteLine($"Items received: {itemNames.Count}");

			var items = new List<BillItemVM>();
			for (int i = 0; i < itemNames.Count; i++)
			{
				if (!string.IsNullOrEmpty(itemNames[i]))
				{
					items.Add(new BillItemVM
					{
						ItemName = itemNames[i],
						Dozen = i < dozens.Count ? dozens[i] : 0,
						Pieces = i < piecess.Count ? piecess[i] : 0,
						Price = i < prices.Count ? prices[i] : 0,
						TotalAmount = i < totals.Count ? totals[i] : 0
					});
				}
			}

			if (items.Count == 0)
			{
				ModelState.AddModelError("", "At least one item is required");
				model.Customers = await GetCustomers();
				return View(model);
			}

			try
			{
				decimal totalAmount = items.Sum(i => i.TotalAmount);

				var sender = new Sender
				{
					Date = model.Date,
					PartyId = model.PartyId,
					TotalAmount = totalAmount,
					Balance = totalAmount,
					TotalItems = items.Count,
					Notes = model.Notes ?? "",
					CreatedBy = GetCurrentUserId(),
					CreatedAt = DateTime.Now
				};

				_context.Senders.Add(sender);
				await _context.SaveChangesAsync();

				foreach (var item in items)
				{
					var billItem = new BillItem
					{
						SenderId = sender.Id,
						ItemName = item.ItemName ?? "",
						Dozen = item.Dozen,
						Pieces = item.Pieces,
						Price = item.Price,
						TotalAmount = item.TotalAmount
					};
					_context.BillItems.Add(billItem);
				}

				await _context.SaveChangesAsync();

				if (_balanceService != null)
				{
					await _balanceService.UpdateSenderBalance(sender.Id);
					await _balanceService.UpdatePartyBalance(model.PartyId);
				}

				TempData["Success"] = $"✅ Bill #{sender.Id} created with {items.Count} items! Total: ₨ {totalAmount:N0}";
				return RedirectToAction(nameof(Index));
			}
			catch (Exception ex)
			{
				TempData["Error"] = $"Save failed: {ex.Message}";
				model.Customers = await GetCustomers();
				return View(model);
			}
		}

		// 🔥 FIXED: Print Bill - Gets ALL items
		public async Task<IActionResult> Print(int id)
		{
			if (!IsLoggedIn()) return RedirectToAction("Login", "Auth");

			var sender = await _context.Senders
				.Include(s => s.Party)
				.FirstOrDefaultAsync(s => s.Id == id);

			if (sender == null)
			{
				TempData["Error"] = "Bill not found!";
				return RedirectToAction(nameof(Index));
			}

			// 🔥 Get ALL items for this bill - no Take, no Skip
			var items = await _context.BillItems
				.Where(i => i.SenderId == id)
				.OrderBy(i => i.Id)
				.ToListAsync();

			var payments = await _context.Receivers
				.Where(r => r.SenderId == id)
				.OrderByDescending(r => r.Date)
				.ToListAsync();

			ViewBag.Items = items;
			ViewBag.Payments = payments;
			ViewBag.TotalPaid = payments.Sum(p => p.Amount);
			ViewBag.ItemCount = items.Count;

			// Log for debugging
			Console.WriteLine($"Print - Bill #{id}: {items.Count} items found");

			return View("PrintMultiBill", sender);
		}

		// GET: Print Multi-Item Bill
		public async Task<IActionResult> PrintMultiBill(int id)
		{
			return await Print(id);
		}

		// GET: Edit Sender
		public async Task<IActionResult> Edit(int id)
		{
			if (!IsLoggedIn()) return RedirectToAction("Login", "Auth");

			var sender = await _context.Senders.FindAsync(id);
			if (sender == null)
			{
				TempData["Error"] = "Bill not found!";
				return RedirectToAction(nameof(Index));
			}

			var model = new SenderVM
			{
				Id = sender.Id,
				Date = sender.Date,
				PartyId = sender.PartyId,
				Dozen = sender.Dozen,
				Pieces = sender.Pieces,
				TotalAmount = sender.TotalAmount,
				Balance = sender.Balance,
				Notes = sender.Notes,
				Customers = await GetCustomers()
			};

			return View(model);
		}

		// POST: Edit Sender
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(int id, SenderVM model)
		{
			if (!IsLoggedIn()) return RedirectToAction("Login", "Auth");

			if (id != model.Id) return NotFound();

			try
			{
				var sender = await _context.Senders.FindAsync(id);
				if (sender == null)
				{
					TempData["Error"] = "Bill not found!";
					return RedirectToAction(nameof(Index));
				}

				var oldPartyId = sender.PartyId;

				sender.Date = model.Date != default ? model.Date : DateTime.Now;
				sender.PartyId = model.PartyId > 0 ? model.PartyId : 1;
				sender.Dozen = model.Dozen > 0 ? model.Dozen : 0;
				sender.Pieces = model.Pieces > 0 ? model.Pieces : 0;
				sender.TotalAmount = model.TotalAmount > 0 ? model.TotalAmount : 0;
				sender.Notes = model.Notes?.Trim() ?? "";

				var totalReceived = await _context.Receivers
					.Where(r => r.SenderId == id)
					.SumAsync(r => (decimal?)r.Amount) ?? 0;
				sender.Balance = model.TotalAmount - totalReceived;

				_context.Update(sender);
				await _context.SaveChangesAsync();

				if (_balanceService != null)
				{
					if (oldPartyId != model.PartyId)
					{
						await _balanceService.UpdatePartyBalance(oldPartyId);
					}
					await _balanceService.UpdatePartyBalance(model.PartyId);
					await _balanceService.UpdateSenderBalance(id);
				}

				TempData["Success"] = "Bill updated successfully!";
				return RedirectToAction(nameof(Index));
			}
			catch (Exception ex)
			{
				TempData["Error"] = $"Update failed: {ex.Message}";
				model.Customers = await GetCustomers();
				return View(model);
			}
		}

	

		// GET: Delete Sender
		public async Task<IActionResult> Delete(int id)
		{
			if (!IsLoggedIn()) return RedirectToAction("Login", "Auth");

			var sender = await _context.Senders
				.Include(s => s.Party)
				.Include(s => s.Receivers)
				.FirstOrDefaultAsync(s => s.Id == id);

			if (sender == null)
			{
				TempData["Error"] = "Bill not found!";
				return RedirectToAction(nameof(Index));
			}

			ViewBag.HasReceivers = sender.Receivers.Any();
			ViewBag.ReceiverCount = sender.Receivers.Count();
			ViewBag.TotalReceived = sender.Receivers.Sum(r => r.Amount);

			return View(sender);
		}

		// POST: Delete Sender
		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed(int id)
		{
			if (!IsLoggedIn()) return RedirectToAction("Login", "Auth");

			var sender = await _context.Senders
				.Include(s => s.Receivers)
				.FirstOrDefaultAsync(s => s.Id == id);

			if (sender != null)
			{
				var partyId = sender.PartyId;

				if (sender.Receivers.Any())
					_context.Receivers.RemoveRange(sender.Receivers);

				var items = await _context.BillItems.Where(i => i.SenderId == id).ToListAsync();
				if (items.Any())
					_context.BillItems.RemoveRange(items);

				_context.Senders.Remove(sender);
				await _context.SaveChangesAsync();

				await _balanceService.UpdatePartyBalance(partyId);

				TempData["Success"] = "Bill deleted successfully!";
			}

			return RedirectToAction(nameof(Index));
		}

		// GET: Bill Details with Payment History
		public async Task<IActionResult> Details(int id)
		{
			if (!IsLoggedIn()) return RedirectToAction("Login", "Auth");

			var sender = await _context.Senders
				.Include(s => s.Party)
				.FirstOrDefaultAsync(s => s.Id == id);

			if (sender == null)
			{
				TempData["Error"] = "Bill not found!";
				return RedirectToAction(nameof(Index));
			}

			var payments = await _context.Receivers
				.Where(r => r.SenderId == id)
				.OrderByDescending(r => r.Date)
				.ToListAsync();

			var items = await _context.BillItems
				.Where(i => i.SenderId == id)
				.ToListAsync();

			ViewBag.Payments = payments;
			ViewBag.Items = items;
			ViewBag.TotalPaid = payments.Sum(p => p.Amount);

			return View(sender);
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

		private async Task<List<CustomerBalanceVM>> GetCustomerBalances()
		{
			var customers = await _context.Parties
				.Where(p => p.PartyType == "Customer" && p.IsActive)
				.ToListAsync();

			var balances = new List<CustomerBalanceVM>();
			foreach (var customer in customers)
			{
				var totalSales = await _context.Senders
					.Where(s => s.PartyId == customer.PartyId)
					.SumAsync(s => (decimal?)s.TotalAmount) ?? 0;

				var totalReceived = await _context.Receivers
					.Where(r => r.PartyId == customer.PartyId)
					.SumAsync(r => (decimal?)r.Amount) ?? 0;

				var billCount = await _context.Senders
					.CountAsync(s => s.PartyId == customer.PartyId);

				balances.Add(new CustomerBalanceVM
				{
					PartyId = customer.PartyId,
					PartyName = customer.PartyName ?? "",
					TotalSales = totalSales,
					TotalReceived = totalReceived,
					Balance = totalSales - totalReceived,
					BillCount = billCount
				});
			}

			return balances.OrderByDescending(b => b.Balance).ToList();
		}
	}
}