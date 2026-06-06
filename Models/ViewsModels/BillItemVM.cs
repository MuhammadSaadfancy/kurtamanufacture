using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FashionPro.Models.ViewModels
{
	public class BillItemVM
	{
		public int Id { get; set; }

		[Required(ErrorMessage = "Item name is required")]
		[Display(Name = "Item Name")]
		public string ItemName { get; set; } = "";

		[Display(Name = "Dozen")]
		[Range(0, 999999)]
		public decimal? Dozen { get; set; }

		[Display(Name = "Pieces")]
		[Range(0, 999999)]
		public decimal? Pieces { get; set; }

		[Required(ErrorMessage = "Price is required")]
		[Range(1, 999999999)]
		[Display(Name = "Price per unit")]
		public decimal Price { get; set; }

		[Display(Name = "Total")]
		public decimal TotalAmount { get; set; }
	}

	public class MultiItemBillVM
	{
		public int Id { get; set; }
		public DateTime Date { get; set; } = DateTime.Now;
		public int PartyId { get; set; }
		public string PartyName { get; set; } = "";
		public string Notes { get; set; } = "";
		public decimal TotalAmount { get; set; }
		public decimal Balance { get; set; }

		[BindProperty]
		public List<BillItemVM> Items { get; set; } = new List<BillItemVM>();

		public List<SelectListItem> Customers { get; set; } = new List<SelectListItem>();
	}
}