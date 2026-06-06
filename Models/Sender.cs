using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FashionPro.Models
{
	[Table("Senders")]
	public class Sender
	{
		[Key]
		public int Id { get; set; }

		[Required]
		public DateTime Date { get; set; } = DateTime.Now;

		[Required]
		public int PartyId { get; set; }

		public decimal? Dozen { get; set; }

		public decimal? Pieces { get; set; }

		[Required]
		public decimal TotalAmount { get; set; }

		[Required]
		public decimal Balance { get; set; }

		[MaxLength(500)]
		public string Notes { get; set; } = "";

		public int TotalItems { get; set; } = 0;

		public int? CreatedBy { get; set; }

		public DateTime CreatedAt { get; set; } = DateTime.Now;

		// Navigation Properties
		[ForeignKey("PartyId")]
		public virtual Party? Party { get; set; }

		[ForeignKey("CreatedBy")]
		public virtual User? User { get; set; }

		public virtual ICollection<Receiver>? Receivers { get; set; }
		//public DateTime UpdatedAt { get; internal set; }
	}
}