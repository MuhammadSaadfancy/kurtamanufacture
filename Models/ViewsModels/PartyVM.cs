using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FashionPro.Models.ViewModels
{
	public class PartyVM
	{
		public int PartyId { get; set; }

		[Required(ErrorMessage = "Party name is required")]
		[Display(Name = "Party Name")]
		[MaxLength(150)]
		public string PartyName { get; set; } = "";

		[Required(ErrorMessage = "Party type is required")]
		[Display(Name = "Party Type")]
		public string PartyType { get; set; } = "";

		[Display(Name = "Phone Number")]
		[MaxLength(20)]
		[RegularExpression(@"^[0-9+\-\(\)\s]*$", ErrorMessage = "Invalid phone number")]
		public string Phone { get; set; } = "";

		[Display(Name = "Address")]
		[MaxLength(300)]
		public string Address { get; set; } = "";

		[Display(Name = "Current Balance")]
		public decimal CurrentBalance { get; set; }

		public bool IsActive { get; set; } = true;

		// For dropdown
		public List<SelectListItem> PartyTypes { get; set; } = new List<SelectListItem>();
	}

	public class PartyListVM
	{
		public int PartyId { get; set; }
		public string PartyName { get; set; } = "";
		public string PartyType { get; set; } = "";
		public string Phone { get; set; } = "";
		public string Address { get; set; } = "";
		public decimal CurrentBalance { get; set; }
		public bool IsActive { get; set; }
		public DateTime CreatedAt { get; set; }

		// Computed Properties
		public string DateDisplay => CreatedAt.ToString("dd-MMM-yyyy");
		public string BalanceColor => CurrentBalance >= 0 ? "text-success" : "text-danger";
		public string BalancePrefix => CurrentBalance >= 0 ? "₨ " : "₨ -";
		public string Status => IsActive ? "Active" : "Inactive";
		public string StatusBadge => IsActive ? "bg-success" : "bg-secondary";
		public string ShortAddress => Address?.Length > 30 ? Address.Substring(0, 30) + "..." : Address;
		public string ShortPhone => Phone ?? "-";
	}
}