using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FashionPro.Models
{
	[Table("FabricPayments")]
	public class FabricPayment
	{
		[Key]
		public int Id { get; set; }

		public DateTime Date { get; set; } = DateTime.Now;

		public int PartyId { get; set; }

		public int? FabricId { get; set; }

		[Required]
		public decimal Amount { get; set; }

		[MaxLength(50)]
		public string PaymentMethod { get; set; } = "";

		[MaxLength(500)]
		public string Notes { get; set; } = "";

		public int? CreatedBy { get; set; }

		public DateTime CreatedAt { get; set; } = DateTime.Now;

		public string PaymentImagePath { get; set; } = "";  // 🔥 FIXED

		[ForeignKey("PartyId")]
		public virtual Party? Party { get; set; }

		[ForeignKey("FabricId")]
		public virtual Fabric? Fabric { get; set; }

		[ForeignKey("CreatedBy")]
		public virtual User? User { get; set; }
	}
}