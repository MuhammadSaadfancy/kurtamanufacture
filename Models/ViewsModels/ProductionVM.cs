using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FashionPro.Models.ViewModels
{
	public class ProductionVM
	{
		public int Id { get; set; }

		[Required(ErrorMessage = "Date is required")]
		[DataType(DataType.Date)]
		public DateTime Date { get; set; } = DateTime.Now;

		[Required(ErrorMessage = "Module type is required")]
		public string ModuleType { get; set; } = "";

		public string SubType { get; set; } = "";

		[Required(ErrorMessage = "Party/Worker is required")]
		public int? PartyId { get; set; }

		public string PartyName { get; set; } = "";

		[Display(Name = "Item Name")]
		public string ItemName { get; set; } = "";

		[Display(Name = "Dozen")]
		public decimal? Dozen { get; set; }

		[Display(Name = "Pieces")]
		public decimal? Pieces { get; set; }

		[Required(ErrorMessage = "Amount is required")]
		[Range(1, 999999999)]
		public decimal Amount { get; set; }

		public decimal Balance { get; set; }

		public string Status { get; set; } = "Pending";

		[MaxLength(500)]
		public string Notes { get; set; } = "";

		public List<SelectListItem> Workers { get; set; } = new List<SelectListItem>();
		public List<SelectListItem> SubTypes { get; set; } = new List<SelectListItem>();
	}

	public class ProductionListVM
	{
		public int Id { get; set; }
		public DateTime Date { get; set; }
		public string ModuleType { get; set; } = "";
		public string SubType { get; set; } = "";
		public string PartyName { get; set; } = "";
		public string ItemInfo { get; set; } = "";
		public decimal Amount { get; set; }
		public decimal Balance { get; set; }
		public string Status { get; set; } = "";



		public string DateDisplay => Date.ToString("dd-MMM-yyyy");
		public string StatusBadge => Status switch
		{
			"Paid" => "bg-success",
			"Partial" => "bg-warning",
			_ => "bg-danger"
		};
		public string BalanceColor => Balance > 0 ? "text-danger" : "text-success";
	}

	public class ProductionPaymentVM
	{
		public int Id { get; set; }

		[Required]
		[DataType(DataType.Date)]
		public DateTime Date { get; set; } = DateTime.Now;

		[Required]
		public int PartyId { get; set; }

		public string PartyName { get; set; } = "";

		[Required]
		public int TransactionId { get; set; }

		public string TransactionReference { get; set; } = "";

		[Required]
		[Range(1, 999999999)]
		public decimal Amount { get; set; }

		[Required]
		public string PaymentMethod { get; set; } = "";

		[MaxLength(500)]
		public string Notes { get; set; } = "";

		public string PaymentImagePath { get; set; } = "";
		public IFormFile PaymentImage { get; set; }

		// 🔥 WORKERS PROPERTY - YEH ADD KIYA
		public List<SelectListItem> Workers { get; set; } = new List<SelectListItem>();
		public List<SelectListItem> PaymentMethods { get; set; } = new List<SelectListItem>();
		public List<SelectListItem> Transactions { get; set; } = new List<SelectListItem>();
	}

	public class ProductionPaymentListVM
	{
		public int Id { get; set; }
		public DateTime Date { get; set; }
		public string PartyName { get; set; } = "";
		public string ModuleType { get; set; } = "";
		public decimal Amount { get; set; }
		public string PaymentMethod { get; set; } = "";

		public string PaymentImagePath { get; set; } = "";
		public string DateDisplay => Date.ToString("dd-MMM-yyyy");
	}

	public class ProductionDashboardVM
	{
		public decimal TotalDesignAmount { get; set; }
		public decimal TotalCuttingAmount { get; set; }
		public decimal TotalCMTAmount { get; set; }
		public decimal TotalButtonAmount { get; set; }
		public decimal TotalEndingAmount { get; set; }
		public decimal TotalPaid { get; set; }
		public decimal TotalBalance { get; set; }
		public List<WorkerBalanceVM> WorkerBalances { get; set; } = new List<WorkerBalanceVM>();
	}

	public class WorkerBalanceVM
	{
		public int PartyId { get; set; }
		public string PartyName { get; set; } = "";
		public string WorkerType { get; set; } = "";
		public decimal TotalAmount { get; set; }
		public decimal TotalPaid { get; set; }
		public decimal Balance { get; set; }
		public string BalanceColor => Balance > 0 ? "text-danger" : "text-success";
	}
}