using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using FashionPro.Data;

namespace FashionPro.Controllers
{
	public class ReportController : Controller
	{
		private readonly ApplicationDbContext _context;

		public ReportController(ApplicationDbContext context)
		{
			_context = context;
		}

		// Check if user is logged in
		private bool IsLoggedIn()
		{
			return HttpContext.Session.GetString("UserId") != null;
		}

		// GET: Dashboard
		public async Task<IActionResult> Dashboard()
		{
			if (!IsLoggedIn())
			{
				return RedirectToAction("Login", "Auth");
			}

			ViewBag.Username = HttpContext.Session.GetString("FullName") ?? HttpContext.Session.GetString("Username");

			// Dashboard Statistics
			var today = DateTime.Today;
			var startOfMonth = new DateTime(today.Year, today.Month, 1);

			// Total Parties
			ViewBag.TotalParties = await _context.Parties.CountAsync(p => p.IsActive);

			// Today's Expenses
			ViewBag.TodayExpenses = await _context.Expenses
				.Where(e => e.Date.Date == today)
				.SumAsync(e => (decimal?)e.Amount) ?? 0;

			// Month Expenses
			ViewBag.MonthExpenses = await _context.Expenses
				.Where(e => e.Date >= startOfMonth)
				.SumAsync(e => (decimal?)e.Amount) ?? 0;

			// Pending Payments to Workers (Production)
			ViewBag.PendingWorkerPayments = await _context.ProductionTransactions
				.Where(p => p.Balance > 0)
				.SumAsync(p => (decimal?)p.Balance) ?? 0;

			// Pending Receivables from Customers (Senders Balance)
			ViewBag.PendingCustomerPayments = await _context.Senders
				.Where(s => s.Balance > 0)
				.SumAsync(s => (decimal?)s.Balance) ?? 0;

			// Total Fabrics Balance (Supplier pending)
			ViewBag.SupplierPending = await _context.Fabrics
				.Where(f => f.Balance > 0)
				.SumAsync(f => (decimal?)f.Balance) ?? 0;

			// Recent Transactions (Last 5)
			ViewBag.RecentExpenses = await _context.Expenses
				.OrderByDescending(e => e.Date)
				.Take(5)
				.ToListAsync();

			ViewBag.RecentProduction = await _context.ProductionTransactions
				.Include(p => p.Party)
				.OrderByDescending(p => p.Date)
				.Take(5)
				.ToListAsync();

			ViewBag.RecentSenders = await _context.Senders
				.Include(s => s.Party)
				.OrderByDescending(s => s.Date)
				.Take(5)
				.ToListAsync();

			return View();
		}
	}
}