using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoctorAppointmentSystem.Domain.Entities
{
	[Table("Users")]
	public class User
	{
		[Key]
		public Guid UserId { get; set; }

		[Required]
		[EmailAddress]
		[MaxLength(150)]
		public string Email { get; set; }

		[Required]
		[MaxLength(500)]
		public string PasswordHash { get; set; }

		[Required]
		public bool IsActive { get; set; } = true;
		public bool RequiresPasswordChange { get; set; } = false;
		[Required]
		public bool IsEmailVerified { get; set; } = false;

		public string? EmailVerificationOtp { get; set; }

		public DateTime? EmailVerificationOtpExpiry { get; set; }

		[Required]
		public DateTime CreatedDate { get; set; } = DateTime.UtcNow;


		public DateTime LastLoginDate { get; set; }
	}
}
