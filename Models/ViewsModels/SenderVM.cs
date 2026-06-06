using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FashionPro.Models.ViewModels
{
	public class SenderVM
	{
		public int Id { get; set; }

		[Required(ErrorMessage = "Date is required")]
		[DataType(DataType.Date)]
		public DateTime Date { get; set; } = DateTime.Now;

		[Required(ErrorMessage = "Customer is required")]
		public int PartyId { get; set; }

		public string PartyName { get; set; }

		[Display(Name = "Dozen")]
		public decimal? Dozen { get; set; }

		[Display(Name = "Pieces")]
		public decimal? Pieces { get; set; }

		[Required(ErrorMessage = "Total amount is required")]
		[Range(1, 999999999, ErrorMessage = "Amount must be greater than 0")]
		[Display(Name = "Total Amount")]
		public decimal TotalAmount { get; set; }

		public decimal Balance { get; set; }

		[MaxLength(500)]
		public string Notes { get; set; }

		public List<SelectListItem> Customers { get; set; }
	}

	public class SenderListVM
	{
		public int Id { get; set; }
		public DateTime Date { get; set; }
		public int PartyId { get; set; }
		public string PartyName { get; set; } = "";
		public string Quantity { get; set; } = "";
		public decimal TotalAmount { get; set; }
		public decimal Balance { get; set; }
		public string Status { get; set; } = "";
		public int TotalItems { get; set; } = 0;

		//public bool IsSelected { get; set; }

		public string DateDisplay => Date.ToString("dd-MMM-yyyy");
		public string BalanceColor => Balance > 0 ? "text-danger" : "text-success";
		public string StatusBadge => Balance == 0 ? "bg-success" : (Balance < TotalAmount ? "bg-warning" : "bg-danger");
	}
	public class CustomerDashboardVM
	{
		public decimal TotalSales { get; set; }
		public decimal TotalReceived { get; set; }
		public decimal TotalPending { get; set; }
		public int TotalCustomers { get; set; }
		public int TotalBills { get; set; }
		public List<CustomerBalanceVM> CustomerBalances { get; set; }
	}

	public class CustomerBalanceVM
	{
		public int PartyId { get; set; }
		public string PartyName { get; set; }
		public decimal TotalSales { get; set; }
		public decimal TotalReceived { get; set; }
		public decimal Balance { get; set; }
		public int BillCount { get; set; }

		public string BalanceColor => Balance > 0 ? "text-danger" : "text-success";
	}
}