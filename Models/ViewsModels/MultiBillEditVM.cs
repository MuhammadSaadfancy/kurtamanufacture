using Microsoft.AspNetCore.Mvc.Rendering;

namespace FashionPro.Models.ViewModels
{
	public class MultiBillEditVM
	{
		public List<int> BillIds { get; set; } = new List<int>();
		public List<Sender> Bills { get; set; } = new List<Sender>();
		public List<SelectListItem> Customers { get; set; } = new List<SelectListItem>();

		// Bulk update options
		public bool UpdateDate { get; set; }
		public DateTime? NewDate { get; set; }

		public bool UpdateCustomer { get; set; }
		public int? SelectedPartyId { get; set; }

		public bool UpdateNotes { get; set; }
		public string NewNotes { get; set; } = "";
	}
}