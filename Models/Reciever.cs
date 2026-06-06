using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FashionPro.Models
{
	[Table("Receivers")]
	public class Receiver
	{
		[Key]
		public int Id { get; set; }

		public DateTime Date { get; set; } = DateTime.Now;

		public int PartyId { get; set; }

		public int SenderId { get; set; }

		[Required]
		public decimal Amount { get; set; }

		[MaxLength(50)]
		public string PaymentMethod { get; set; }

		[MaxLength(500)]
		public string Notes { get; set; }

		public string PaymentImagePath { get; set; } = "";

		public int? CreatedBy { get; set; }

		public DateTime CreatedAt { get; set; } = DateTime.Now;

		[ForeignKey("PartyId")]
		public virtual Party Party { get; set; }

		[ForeignKey("SenderId")]
		public virtual Sender Sender { get; set; }

		[ForeignKey("CreatedBy")]
		public virtual User User { get; set; }
	}
}