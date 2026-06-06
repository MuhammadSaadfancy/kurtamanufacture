using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FashionPro.Models
{
	[Table("ProductionTransactions")]
	public class ProductionTransaction
	{
		[Key]
		public int Id { get; set; }

		public DateTime Date { get; set; } = DateTime.Now;

		[Required]
		[MaxLength(50)]
		public string ModuleType { get; set; }

		[MaxLength(30)]
		public string SubType { get; set; }

		public int? PartyId { get; set; }

		[MaxLength(100)]
		public string ItemName { get; set; }

		public decimal? Dozen { get; set; }

		public decimal? Pieces { get; set; }

		[Required]
		public decimal Amount { get; set; }

		[Required]
		public decimal Balance { get; set; }

		[MaxLength(20)]
		public string Status { get; set; } = "Pending";

		[MaxLength(500)]
		public string Notes { get; set; }

		public int? CreatedBy { get; set; }

		public DateTime CreatedAt { get; set; } = DateTime.Now;

		[ForeignKey("PartyId")]
		public virtual Party Party { get; set; }

		[ForeignKey("CreatedBy")]
		public virtual User User { get; set; }

		public virtual ICollection<ProductionPayment> ProductionPayments { get; set; }
	}
}