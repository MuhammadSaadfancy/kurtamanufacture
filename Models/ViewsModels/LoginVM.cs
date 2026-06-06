using System.ComponentModel.DataAnnotations;

namespace FashionPro.Models.ViewModels
{
	public class LoginVM
	{
		[Required(ErrorMessage = "Username is required")]
		[Display(Name = "Username")]
		public string Username { get; set; }

		[Required(ErrorMessage = "Password is required")]
		[DataType(DataType.Password)]
		[Display(Name = "Password")]
		public string Password { get; set; }

		[Display(Name = "Remember Me")]
		public bool RememberMe { get; set; }
	}

	public class ChangePasswordVM
	{
		[Required]
		public string CurrentPassword { get; set; }

		[Required]
		[MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
		public string NewPassword { get; set; }

		[Required]
		[Compare("NewPassword", ErrorMessage = "Passwords do not match")]
		public string ConfirmPassword { get; set; }
	}
}