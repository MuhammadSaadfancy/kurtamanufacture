using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FashionPro.Models.ViewModels
{
	public class FabricVM
	{
		public int Id { get; set; }

		[Required(ErrorMessage = "Date is required")]
		[DataType(DataType.Date)]
		public DateTime Date { get; set; } = DateTime.Now;

		[Required(ErrorMessage = "Party is required")]
		[Display(Name = "Supplier")]
		public int PartyId { get; set; }

		[Display(Name = "Supplier Name")]
		public string PartyName { get; set; }

		[Display(Name = "Variety")]
		[MaxLength(100)]
		public string Variety { get; set; }

		[Display(Name = "KG")]
		public decimal? KG { get; set; }

		[Display(Name = "Than")]
		public decimal? Than { get; set; }

		[Required(ErrorMessage = "Amount is required")]
		[Range(1, 999999999, ErrorMessage = "Amount must be greater than 0")]
		public decimal Amount { get; set; }

		[Display(Name = "Balance")]
		public decimal Balance { get; set; }

		[MaxLength(500)]
		public string Notes { get; set; }

		// For dropdown
		public List<SelectListItem> Suppliers { get; set; }
	}

	public class FabricListVM
	{
		public int Id { get; set; }
		public DateTime Date { get; set; }

		public int PartyId { get; set; }
		public string PartyName { get; set; }

		public string Variety { get; set; }
		public string Quantity { get; set; }
		public decimal Amount { get; set; }
		public decimal Balance { get; set; }
		public string Notes { get; set; }
		public string DateDisplay => Date.ToString("dd-MMM-yyyy");
		public string BalanceColor => Balance > 0 ? "text-danger" : "text-success";
	}

	public class FabricPaymentVM
	{
		public int Id { get; set; }

		[Required(ErrorMessage = "Date is required")]
		[DataType(DataType.Date)]
		public DateTime Date { get; set; } = DateTime.Now;

		[Required(ErrorMessage = "Party is required")]
		public int PartyId { get; set; }

		public string PartyName { get; set; }

		public int? FabricId { get; set; }

		[Display(Name = "Fabric Reference")]
		public string FabricReference { get; set; }

		[Required(ErrorMessage = "Amount is required")]
		[Range(1, 999999999, ErrorMessage = "Amount must be greater than 0")]
		public decimal Amount { get; set; }

		[Required(ErrorMessage = "Payment method is required")]
		[Display(Name = "Payment Method")]
		public string PaymentMethod { get; set; }

		[MaxLength(500)]
		public string Notes { get; set; }

		public string PaymentImagePath { get; set; } = "";
		public IFormFile PaymentImage { get; set; }

		public List<SelectListItem> PaymentMethods { get; set; }
		public List<SelectListItem> Suppliers { get; set; }
	}

	public class FabricPaymentListVM
	{
		public int Id { get; set; }
		public DateTime Date { get; set; }
		public string PartyName { get; set; } = "";
		public decimal Amount { get; set; }
		public string PaymentMethod { get; set; } = "";
		public string FabricReference { get; set; } = "";
		public string Notes { get; set; } = "";
		public string PaymentImagePath { get; set; } = "";     // 🔥 YEH LINE ADD KARO

		public string DateDisplay => Date.ToString("dd-MMM-yyyy");
	}

	public class FabricDashboardVM
	{
		public decimal TotalFabricAmount { get; set; }
		public decimal TotalPaid { get; set; }
		public decimal TotalBalance { get; set; }
		public int TotalTransactions { get; set; }
		public List<SupplierBalanceVM> SupplierBalances { get; set; }
	}

	public class SupplierBalanceVM
	{
		public int PartyId { get; set; }
		public string PartyName { get; set; }
		public decimal TotalAmount { get; set; }
		public decimal TotalPaid { get; set; }
		public decimal Balance { get; set; }
		public string BalanceColor => Balance > 0 ? "text-danger" : "text-success";
	}
}