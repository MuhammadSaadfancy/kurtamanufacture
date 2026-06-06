using FashionPro.Data;
using FashionPro.Models;
using FashionPro.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FashionPro.Controllers
{
	public class PartyController : Controller
	{
		private readonly ApplicationDbContext _context;

		public PartyController(ApplicationDbContext context)
		{
			_context = context;
		}

		// Check if user is logged in
		private bool IsLoggedIn()
		{
			return HttpContext.Session.GetString("UserId") != null;
		}

		// GET: Party List
		public async Task<IActionResult> Index(string searchTerm, string partyType, int page = 1)
		{
			if (!IsLoggedIn())
			{
				return RedirectToAction("Login", "Auth");
			}

			ViewBag.SearchTerm = searchTerm;
			ViewBag.PartyType = partyType;

			// Get party types for filter dropdown
			var partyTypes = await _context.Parties
				.Select(p => p.PartyType)
				.Distinct()
				.ToListAsync();
			ViewBag.PartyTypes = new SelectList(partyTypes);

			var query = _context.Parties.AsQueryable();

			// Apply filters
			if (!string.IsNullOrEmpty(searchTerm))
			{
				query = query.Where(p => p.PartyName.Contains(searchTerm) ||
										 (p.Phone != null && p.Phone.Contains(searchTerm)));
			}

			if (!string.IsNullOrEmpty(partyType))
			{
				query = query.Where(p => p.PartyType == partyType);
			}

			// Get total count for pagination
			int pageSize = 15;
			int totalRecords = await query.CountAsync();
			int totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

			// Get paginated data with NULL handling
			var parties = await query
				.OrderBy(p => p.PartyName)
				.Skip((page - 1) * pageSize)
				.Take(pageSize)
				.Select(p => new PartyListVM
				{
					PartyId = p.PartyId,
					PartyName = p.PartyName ?? "",
					PartyType = p.PartyType ?? "",
					Phone = p.Phone ?? "",
					Address = p.Address ?? "",
					CurrentBalance = p.CurrentBalance,
					IsActive = p.IsActive,
					CreatedAt = p.CreatedAt
				})
				.ToListAsync();

			ViewBag.CurrentPage = page;
			ViewBag.TotalPages = totalPages;
			ViewBag.TotalRecords = totalRecords;

			return View(parties);
		}

		// GET: Party Create
		public IActionResult Create()
		{
			if (!IsLoggedIn())
			{
				return RedirectToAction("Login", "Auth");
			}

			var model = new PartyVM
			{
				PartyTypes = GetPartyTypes(),
				IsActive = true
			};
			return View(model);
		}

		// POST: Party Create
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(PartyVM model)
		{
			if (!IsLoggedIn())
			{
				return RedirectToAction("Login", "Auth");
			}

			if (ModelState.IsValid)
			{
				var party = new Party
				{
					PartyName = model.PartyName,
					PartyType = model.PartyType,
					Phone = model.Phone ?? "",
					Address = model.Address ?? "",
					CurrentBalance = 0,
					IsActive = model.IsActive,
					CreatedAt = DateTime.Now
				};

				_context.Parties.Add(party);
				await _context.SaveChangesAsync();

				TempData["Success"] = $"Party '{party.PartyName}' created successfully!";
				return RedirectToAction(nameof(Index));
			}

			model.PartyTypes = GetPartyTypes();
			return View(model);
		}

		// GET: Party Edit
		public async Task<IActionResult> Edit(int id)
		{
			if (!IsLoggedIn())
			{
				return RedirectToAction("Login", "Auth");
			}

			var party = await _context.Parties.FindAsync(id);
			if (party == null)
			{
				TempData["Error"] = "Party not found!";
				return RedirectToAction(nameof(Index));
			}

			var model = new PartyVM
			{
				PartyId = party.PartyId,
				PartyName = party.PartyName,
				PartyType = party.PartyType,
				Phone = party.Phone ?? "",
				Address = party.Address ?? "",
				CurrentBalance = party.CurrentBalance,
				IsActive = party.IsActive,
				PartyTypes = GetPartyTypes()
			};

			return View(model);
		}

		// POST: Party Edit
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(int id, PartyVM model)
		{
			if (!IsLoggedIn())
			{
				return RedirectToAction("Login", "Auth");
			}

			if (id != model.PartyId)
			{
				return NotFound();
			}

			if (ModelState.IsValid)
			{
				try
				{
					var party = await _context.Parties.FindAsync(id);
					if (party == null)
					{
						TempData["Error"] = "Party not found!";
						return RedirectToAction(nameof(Index));
					}

					party.PartyName = model.PartyName;
					party.PartyType = model.PartyType;
					party.Phone = model.Phone ?? "";
					party.Address = model.Address ?? "";
					party.IsActive = model.IsActive;

					_context.Update(party);
					await _context.SaveChangesAsync();

					TempData["Success"] = $"Party '{party.PartyName}' updated successfully!";
					return RedirectToAction(nameof(Index));
				}
				catch (DbUpdateConcurrencyException)
				{
					if (!PartyExists(model.PartyId))
					{
						return NotFound();
					}
					throw;
				}
			}

			model.PartyTypes = GetPartyTypes();
			return View(model);
		}

		// GET: Party Delete (Confirmation)
		public async Task<IActionResult> Delete(int id)
		{
			if (!IsLoggedIn())
			{
				return RedirectToAction("Login", "Auth");
			}

			var party = await _context.Parties.FindAsync(id);
			if (party == null)
			{
				TempData["Error"] = "Party not found!";
				return RedirectToAction(nameof(Index));
			}

			// Check if party has any transactions
			var hasFabricTransactions = await _context.Fabrics.AnyAsync(f => f.PartyId == id);
			var hasProductionTransactions = await _context.ProductionTransactions.AnyAsync(p => p.PartyId == id);
			var hasSenderTransactions = await _context.Senders.AnyAsync(s => s.PartyId == id);
			var hasPaymentTransactions = await _context.FabricPayments.AnyAsync(fp => fp.PartyId == id) ||
										 await _context.ProductionPayments.AnyAsync(pp => pp.PartyId == id) ||
										 await _context.Receivers.AnyAsync(r => r.PartyId == id);

			ViewBag.HasTransactions = hasFabricTransactions || hasProductionTransactions ||
									   hasSenderTransactions || hasPaymentTransactions;
			ViewBag.WarningMessage = "This party has transaction history. Deleting may affect reports.";

			return View(party);
		}

		// POST: Party Delete
		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed(int id)
		{
			if (!IsLoggedIn())
			{
				return RedirectToAction("Login", "Auth");
			}

			var party = await _context.Parties
				.Include(p => p.Fabrics)
				.Include(p => p.ProductionTransactions)
				.Include(p => p.Senders)
				.FirstOrDefaultAsync(p => p.PartyId == id);

			if (party == null)
			{
				TempData["Error"] = "Party not found!";
				return RedirectToAction(nameof(Index));
			}

			// Check if has transactions - if yes, just deactivate instead of delete
			bool hasTransactions = party.Fabrics.Any() || party.ProductionTransactions.Any() || party.Senders.Any();

			if (hasTransactions)
			{
				party.IsActive = false;
				_context.Update(party);
				TempData["Warning"] = $"Party '{party.PartyName}' has transactions. It has been deactivated instead of deleted.";
			}
			else
			{
				_context.Parties.Remove(party);
				TempData["Success"] = $"Party '{party.PartyName}' deleted successfully!";
			}

			await _context.SaveChangesAsync();
			return RedirectToAction(nameof(Index));
		}

		// GET: Party Details with Transactions
		public async Task<IActionResult> Details(int id)
		{
			if (!IsLoggedIn())
			{
				return RedirectToAction("Login", "Auth");
			}

			var party = await _context.Parties
				.FirstOrDefaultAsync(p => p.PartyId == id);

			if (party == null)
			{
				TempData["Error"] = "Party not found!";
				return RedirectToAction(nameof(Index));
			}

			// Get related transactions
			var fabrics = await _context.Fabrics
				.Where(f => f.PartyId == id)
				.OrderByDescending(f => f.Date)
				.Take(10)
				.ToListAsync();

			var productions = await _context.ProductionTransactions
				.Where(p => p.PartyId == id)
				.OrderByDescending(p => p.Date)
				.Take(10)
				.ToListAsync();

			var senders = await _context.Senders
				.Where(s => s.PartyId == id)
				.OrderByDescending(s => s.Date)
				.Take(10)
				.ToListAsync();

			ViewBag.Party = party;
			ViewBag.Fabrics = fabrics;
			ViewBag.Productions = productions;
			ViewBag.Senders = senders;

			return View();
		}

		// Helper: Get Party Types
		private List<SelectListItem> GetPartyTypes()
		{
			return new List<SelectListItem>
			{
				new SelectListItem { Value = "Supplier", Text = "Supplier (Kapra Dene Wala)" },
				new SelectListItem { Value = "CMTWorker", Text = "CMT Worker (Silai Wala)" },
				new SelectListItem { Value = "CuttingMaster", Text = "Cutting Master" },
				new SelectListItem { Value = "Designer", Text = "Designer" },
				new SelectListItem { Value = "ButtonWorker", Text = "Button Worker" },
				new SelectListItem { Value = "PressWorker", Text = "Press Worker" },
				new SelectListItem { Value = "Customer", Text = "Customer" }
			};
		}

		private bool PartyExists(int id)
		{
			return _context.Parties.Any(e => e.PartyId == id);
		}
	}
}