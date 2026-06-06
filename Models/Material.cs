using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FashionPro.Models
{
	[Table("Materials")]
	public class Material
	{
		[Key]
		public int Id { get; set; }

		public DateTime Date { get; set; } = DateTime.Now;

		[Required]
		[MaxLength(150)]
		public string MaterialName { get; set; }

		public decimal? Quantity { get; set; }

		[MaxLength(20)]
		public string Unit { get; set; }

		[Required]
		public decimal Amount { get; set; }

		[MaxLength(500)]
		public string Notes { get; set; }

		public int? CreatedBy { get; set; }

		public DateTime CreatedAt { get; set; } = DateTime.Now;

		[ForeignKey("CreatedBy")]
		public virtual User User { get; set; }
	}
}