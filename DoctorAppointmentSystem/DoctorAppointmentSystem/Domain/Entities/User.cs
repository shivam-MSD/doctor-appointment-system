using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoctorAppointmentSystem.Domain.Entities
{
	/// <summary>
	/// Represents a core user identity record within the HealSync medical system.
	/// Stores authentication credentials, account status, email verification OTP state, and login timestamps.
	/// </summary>
	[Table("Users")]
	public class User
	{
		/// <summary>
		/// Gets or sets the unique primary key identifier for the user account.
		/// </summary>
		[Key]
		public Guid UserId { get; set; }

		/// <summary>
		/// Gets or sets the primary email address used for login authentication and system notifications.
		/// </summary>
		[Required]
		[EmailAddress]
		[MaxLength(150)]
		public string Email { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the user account is active and permitted to access the system.
		/// </summary>
		[Required]
		public bool IsActive { get; set; } = true;

		/// <summary>
		/// Gets or sets a value indicating whether the user is required to change their temporary password upon next login.
		/// </summary>
		public bool RequiresPasswordChange { get; set; } = false;

		/// <summary>
		/// Gets or sets a value indicating whether the user's email address has been verified via OTP.
		/// </summary>
		[Required]
		public bool IsEmailVerified { get; set; } = false;

		/// <summary>
		/// Gets or sets the active One-Time Password (OTP) code generated for email verification or password reset.
		/// </summary>
		public string? EmailVerificationOtp { get; set; }

		/// <summary>
		/// Gets or sets the expiration UTC timestamp for the current active OTP code.
		/// </summary>
		public DateTime? EmailVerificationOtpExpiry { get; set; }

		/// <summary>
		/// Gets or sets the UTC creation timestamp when the user account was registered.
		/// </summary>
		[Required]
		public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

		/// <summary>
		/// Gets or sets the UTC timestamp of the user's most recent successful login session.
		/// </summary>
		public DateTime LastLoginDate { get; set; }
	}
}
