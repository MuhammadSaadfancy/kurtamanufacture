using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FashionPro.Models
{
	[Table("Users")]
	public class User
	{
		[Key]
		public int UserId { get; set; }

		[Required]
		[MaxLength(50)]
		public string Username { get; set; }

		[Required]
		[MaxLength(255)]
		public string PasswordHash { get; set; }

		[MaxLength(100)]
		public string FullName { get; set; }

		public bool IsActive { get; set; } = true;

		public DateTime CreatedAt { get; set; } = DateTime.Now;
	}
}