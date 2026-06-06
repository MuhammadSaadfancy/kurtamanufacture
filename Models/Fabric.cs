using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FashionPro.Models
{
	[Table("Fabrics")]
	public class Fabric
	{
		[Key]
		public int Id { get; set; }

		public DateTime Date { get; set; } = DateTime.Now;

		public int PartyId { get; set; }

		[MaxLength(100)]
		public string Variety { get; set; }

		public decimal? KG { get; set; }

		public decimal? Than { get; set; }

		[Required]
		public decimal Amount { get; set; }

		[Required]
		public decimal Balance { get; set; }

		[MaxLength(500)]
		public string Notes { get; set; }

		public string PaymentImagePath { get; set; } = "";

		public int? CreatedBy { get; set; }

		public DateTime CreatedAt { get; set; } = DateTime.Now;

		[ForeignKey("PartyId")]
		public virtual Party Party { get; set; }

		[ForeignKey("CreatedBy")]
		public virtual User User { get; set; }

		public virtual ICollection<FabricPayment> FabricPayments { get; set; }
	}
}