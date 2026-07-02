using System.ComponentModel.DataAnnotations;

namespace DoctorAppointmentSystem.Application.DTOs
{
	public class LoginDto
	{
		[Required]
		[EmailAddress]
		public string Email { get; set; }

		[Required]
		public string Password { get; set; }
	}

	public class RegisterDto
	{
		[Required]
		[EmailAddress]
		public string Email { get; set; }

		[Required]
		[MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
		public string Password { get; set; }

		[Required]
		public string FirstName { get; set; }

		[Required]
		public string LastName { get; set; }

		[Required]
		[Phone]
		public string MobileNo { get; set; }

		[Required]
		public string Role { get; set; } // e.g., "Doctor", "Patient"
	}

	public class AuthResponseDto
	{
		public string Token { get; set; }
		public string RefreshToken { get; set; }
		public Guid UserId { get; set; }
		public string Email { get; set; }
		public string Role { get; set; }
	}
}
