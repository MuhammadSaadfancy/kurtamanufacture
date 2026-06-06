using System.ComponentModel.DataAnnotations;

namespace FashionPro.Models.ViewModels
{
	public class ExpenseVM
	{
		public int Id { get; set; }

		[Required(ErrorMessage = "Date is required")]
		[DataType(DataType.Date)]
		[Display(Name = "Expense Date")]
		public DateTime Date { get; set; } = DateTime.Now;

		[Required(ErrorMessage = "Expense name is required")]
		[Display(Name = "Expense Name")]
		[MaxLength(200)]
		public string ExpenseName { get; set; }

		[Required(ErrorMessage = "Amount is required")]
		[Range(1, 999999999, ErrorMessage = "Amount must be greater than 0")]
		[Display(Name = "Amount")]
		[DataType(DataType.Currency)]
		public decimal Amount { get; set; }

		[Display(Name = "Notes")]
		[MaxLength(500)]
		public string Notes { get; set; }

		public DateTime CreatedAt { get; set; }
		public string CreatedByUsername { get; set; }
	}

	public class ExpenseListVM
	{
		public int Id { get; set; }
		public DateTime Date { get; set; }
		public string ExpenseName { get; set; }
		public decimal Amount { get; set; }
		public string Notes { get; set; }
		public DateTime CreatedAt { get; set; }
		public string DateDisplay => Date.ToString("dd-MMM-yyyy");
		public string ShortNotes => Notes?.Length > 30 ? Notes.Substring(0, 30) + "..." : Notes;
	}

	public class ExpenseSummaryVM
	{
		public decimal TodayTotal { get; set; }
		public decimal WeekTotal { get; set; }
		public decimal MonthTotal { get; set; }
		public int TodayCount { get; set; }
		public int WeekCount { get; set; }
		public int MonthCount { get; set; }
		public List<ExpenseByCategoryVM> TopExpenses { get; set; }
	}

	public class ExpenseByCategoryVM
	{
		public string ExpenseName { get; set; }
		public decimal TotalAmount { get; set; }
		public int Count { get; set; }
	}
}