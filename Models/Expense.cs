using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FashionPro.Models
{
	[Table("Expenses")]
	public class Expense
	{
		[Key]
		public int Id { get; set; }

		public DateTime Date { get; set; } = DateTime.Now;

		[Required]
		[MaxLength(200)]
		public string ExpenseName { get; set; }

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