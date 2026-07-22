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

		public string? Role { get; set; }
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
		[Compare("Password", ErrorMessage = "Password and confirmation password do not match.")]
		public string ConfirmPassword { get; set; }

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

	public class DoctorSignUpDto
	{
		[Required]
		[EmailAddress]
		public string Email { get; set; }

		// Password is optional for Approach 1 (auto-generated on SuperAdmin approval)
		public string? Password { get; set; }

		[Required]
		public string FirstName { get; set; }

		[Required]
		public string LastName { get; set; }

		[Required]
		[Phone]
		public string MobileNo { get; set; }

		[Required]
		public string Gender { get; set; }

		[Required]
		public DateTime DOB { get; set; }

		[Required]
		public string Qualification { get; set; }

		[Required]
		public string LicenceNumber { get; set; }

		[Required]
		public int YearsOfExperience { get; set; }

		[Required]
		public double ConsultationFee { get; set; }

		[Required]
		public Guid SpecializationId { get; set; }
	}

	public class CheckEmailDto
	{
		[Required]
		[EmailAddress]
		public string Email { get; set; }
	}

	// --- Password Management DTOs ---

	public class ForgotPasswordDto
	{
		[Required]
		[EmailAddress]
		public string Email { get; set; }
	}

	public class ResetPasswordDto
	{
		[Required]
		[EmailAddress]
		public string Email { get; set; }

		[Required]
		[MinLength(6)]
		[MaxLength(6)]
		public string Otp { get; set; }

		[Required]
		[MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
		public string NewPassword { get; set; }
	}

	public class InitiatePasswordUpdateDto
	{
		[Required]
		public string CurrentPassword { get; set; }
	}

	public class UpdatePasswordDto
	{
		[Required]
		public string Otp { get; set; }

		[Required]
		[MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
		public string NewPassword { get; set; }
	}

	public class AuthResponseDto
	{
		public string Token { get; set; }
		public string RefreshToken { get; set; }
		public Guid UserId { get; set; }
		public string Email { get; set; }
		public string Role { get; set; }
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public Guid? ProfileId { get; set; }
		public bool RequiresPasswordChange { get; set; }
	}
}
