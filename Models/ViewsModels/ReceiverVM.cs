using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FashionPro.Models.ViewModels
{
	public class ReceiverVM
	{
		public int Id { get; set; }

		[Required(ErrorMessage = "Date is required")]
		[DataType(DataType.Date)]
		[Display(Name = "Payment Date")]
		public DateTime Date { get; set; } = DateTime.Now;

		[Required(ErrorMessage = "Customer is required")]
		[Display(Name = "Customer")]
		public int PartyId { get; set; }

		[Display(Name = "Customer Name")]
		public string PartyName { get; set; }

		[Required(ErrorMessage = "Bill is required")]
		[Display(Name = "Select Bill")]
		public int SenderId { get; set; }

		[Display(Name = "Bill Reference")]
		public string BillReference { get; set; }

		[Required(ErrorMessage = "Amount is required")]
		[Range(1, 999999999, ErrorMessage = "Amount must be greater than 0")]
		[Display(Name = "Payment Amount")]
		public decimal Amount { get; set; }

		[Required(ErrorMessage = "Payment method is required")]
		[Display(Name = "Payment Method")]
		public string PaymentMethod { get; set; }

		[MaxLength(500)]
		[Display(Name = "Notes")]
		public string Notes { get; set; }

		public string PaymentImagePath { get; set; } = "";
		public IFormFile PaymentImage { get; set; }

		// For Dropdowns
		public List<SelectListItem> Customers { get; set; }
		public List<SelectListItem> PaymentMethods { get; set; }
		public List<SelectListItem> PendingBills { get; set; }
	}

	public class ReceiverListVM
	{
		public int Id { get; set; }
		public DateTime Date { get; set; }
		public string PartyName { get; set; }
		public decimal Amount { get; set; }
		public string PaymentMethod { get; set; }
		public string BillReference { get; set; }

		public string PaymentImagePath { get; set; } = "";

		// Computed Properties
		public string DateDisplay => Date.ToString("dd-MMM-yyyy");
		public string AmountDisplay => $"₨ {Amount:N0}";
	}
}