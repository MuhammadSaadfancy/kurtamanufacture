using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FashionPro.Models.ViewModels
{
	public class MaterialVM
	{
		public int Id { get; set; }

		[Required(ErrorMessage = "Date is required")]
		[DataType(DataType.Date)]
		public DateTime Date { get; set; } = DateTime.Now;

		[Required(ErrorMessage = "Material name is required")]
		[Display(Name = "Material Name")]
		[MaxLength(150)]
		public string MaterialName { get; set; }

		[Display(Name = "Quantity")]
		public decimal? Quantity { get; set; }

		[Display(Name = "Unit")]
		[MaxLength(20)]
		public string Unit { get; set; }

		[Required(ErrorMessage = "Amount is required")]
		[Range(1, 999999999, ErrorMessage = "Amount must be greater than 0")]
		[Display(Name = "Amount")]
		public decimal Amount { get; set; }

		[MaxLength(500)]
		public string Notes { get; set; }

		public List<SelectListItem> Units { get; set; }
	}

	public class MaterialListVM
	{
		public int Id { get; set; }
		public DateTime Date { get; set; }
		public string MaterialName { get; set; }
		public string QuantityDisplay { get; set; }
		public decimal Amount { get; set; }
		public string Notes { get; set; }
		public string DateDisplay => Date.ToString("dd-MMM-yyyy");
		public string ShortNotes => Notes?.Length > 30 ? Notes.Substring(0, 30) + "..." : Notes;
	}

	public class MaterialDashboardVM
	{
		public decimal TotalAmount { get; set; }
		public int TotalTransactions { get; set; }
		public decimal TodayAmount { get; set; }
		public decimal MonthAmount { get; set; }
		public List<MaterialByCategoryVM> TopMaterials { get; set; }
	}

	public class MaterialByCategoryVM
	{
		public string MaterialName { get; set; }
		public decimal TotalAmount { get; set; }
		public int Count { get; set; }
	}
}