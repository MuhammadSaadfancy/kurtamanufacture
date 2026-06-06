using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FashionPro.Models
{
	[Table("BillItems")]
	public class BillItem
	{
		[Key]
		public int Id { get; set; }

		[Required]
		public int SenderId { get; set; }

		[Required]
		[MaxLength(200)]
		[Display(Name = "Item Name")]
		public string ItemName { get; set; } = "";

		[Display(Name = "Dozen")]
		public decimal? Dozen { get; set; }

		[Display(Name = "Pieces")]
		public decimal? Pieces { get; set; }

		[Required]
		[Display(Name = "Price")]
		public decimal Price { get; set; }

		[Required]
		[Display(Name = "Total Amount")]
		public decimal TotalAmount { get; set; }

		[ForeignKey("SenderId")]
		public virtual Sender? Sender { get; set; }
	}
}