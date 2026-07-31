using System;
using System.ComponentModel.DataAnnotations;

namespace DoctorAppointmentSystem.Application.DTOs
{
	/// <summary>
	/// Data transfer object carrying credentials for user login authentication.
	/// </summary>
	public class LoginDto
	{
		/// <summary>
		/// Gets or sets the primary email address of the user.
		/// </summary>
		[Required]
		[EmailAddress]
		public string Email { get; set; }

		/// <summary>
		/// Gets or sets the secret password string.
		/// </summary>
		[Required]
		public string Password { get; set; }

		/// <summary>
		/// Gets or sets the target role portal string (optional, e.g. "Doctor", "Patient").
		/// </summary>
		public string? Role { get; set; }
	}

	/// <summary>
	/// Data transfer object carrying sign-up details for new patient registrations.
	/// </summary>
	public class RegisterDto
	{
		/// <summary>
		/// Gets or sets the email address for the new account.
		/// </summary>
		[Required]
		[EmailAddress]
		public string Email { get; set; }

		/// <summary>
		/// Gets or sets the initial password string (minimum 6 characters).
		/// </summary>
		[Required]
		[MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
		public string Password { get; set; }

		/// <summary>
		/// Gets or sets the password confirmation string matching the Password field.
		/// </summary>
		[Required]
		[Compare("Password", ErrorMessage = "Password and confirmation password do not match.")]
		public string ConfirmPassword { get; set; }

		/// <summary>
		/// Gets or sets the first name of the registering patient.
		/// </summary>
		[Required]
		public string FirstName { get; set; }

		/// <summary>
		/// Gets or sets the last name of the registering patient.
		/// </summary>
		[Required]
		public string LastName { get; set; }

		/// <summary>
		/// Gets or sets the primary mobile contact number of the registering patient.
		/// </summary>
		[Required]
		[Phone]
		public string MobileNo { get; set; }

		/// <summary>
		/// Gets or sets the target account role ("Patient").
		/// </summary>
		[Required]
		public string Role { get; set; }
	}

	/// <summary>
	/// Data transfer object carrying medical onboarding application data for new doctor sign-ups.
	/// </summary>
	public class DoctorSignUpDto
	{
		/// <summary>
		/// Gets or sets the doctor's primary professional email address.
		/// </summary>
		[Required]
		[EmailAddress]
		public string Email { get; set; }

		/// <summary>
		/// Gets or sets the optional initial password string (auto-generated upon approval if omitted).
		/// </summary>
		public string? Password { get; set; }

		/// <summary>
		/// Gets or sets the first name of the doctor.
		/// </summary>
		[Required]
		public string FirstName { get; set; }

		/// <summary>
		/// Gets or sets the last name of the doctor.
		/// </summary>
		[Required]
		public string LastName { get; set; }

		/// <summary>
		/// Gets or sets the mobile contact number of the doctor.
		/// </summary>
		[Required]
		[Phone]
		public string MobileNo { get; set; }

		/// <summary>
		/// Gets or sets the gender string ("Male", "Female", "Other").
		/// </summary>
		[Required]
		public string Gender { get; set; }

		/// <summary>
		/// Gets or sets the date of birth of the doctor.
		/// </summary>
		[Required]
		public DateTime DOB { get; set; }

		/// <summary>
		/// Gets or sets the academic qualifications and medical degrees held by the doctor.
		/// </summary>
		[Required]
		public string Qualification { get; set; }

		/// <summary>
		/// Gets or sets the medical council license registration number.
		/// </summary>
		[Required]
		public string LicenceNumber { get; set; }

		/// <summary>
		/// Gets or sets the total years of professional clinical experience.
		/// </summary>
		[Required]
		public int YearsOfExperience { get; set; }

		/// <summary>
		/// Gets or sets the standard consultation fee.
		/// </summary>
		[Required]
		public double ConsultationFee { get; set; }

		/// <summary>
		/// Gets or sets the target medical specialization ID reference.
		/// </summary>
		[Required]
		public Guid SpecializationId { get; set; }
	}

	/// <summary>
	/// Data transfer object used to check if an email address is already registered.
	/// </summary>
	public class CheckEmailDto
	{
		/// <summary>
		/// Gets or sets the email address to verify.
		/// </summary>
		[Required]
		[EmailAddress]
		public string Email { get; set; }

		/// <summary>
		/// Gets or sets the target role portal string (optional).
		/// </summary>
		public string? Role { get; set; }
	}

	/// <summary>
	/// Data transfer object used to initiate a forgot password OTP request.
	/// </summary>
	public class ForgotPasswordDto
	{
		/// <summary>
		/// Gets or sets the email address requesting a password reset.
		/// </summary>
		[Required]
		[EmailAddress]
		public string Email { get; set; }

		/// <summary>
		/// Gets or sets the account role associated with the reset request.
		/// </summary>
		[Required]
		public string Role { get; set; }
	}

	/// <summary>
	/// Data transfer object carrying the OTP verification code and new password payload.
	/// </summary>
	public class ResetPasswordDto
	{
		/// <summary>
		/// Gets or sets the email address for password reset.
		/// </summary>
		[Required]
		[EmailAddress]
		public string Email { get; set; }

		/// <summary>
		/// Gets or sets the 6-digit OTP verification code received via email.
		/// </summary>
		[Required]
		[MinLength(6)]
		[MaxLength(6)]
		public string Otp { get; set; }

		/// <summary>
		/// Gets or sets the new password string.
		/// </summary>
		[Required]
		[MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
		public string NewPassword { get; set; }

		/// <summary>
		/// Gets or sets the account role string.
		/// </summary>
		[Required]
		public string Role { get; set; }
	}

	/// <summary>
	/// Data transfer object carrying the current password to initiate a password change while logged in.
	/// </summary>
	public class InitiatePasswordUpdateDto
	{
		/// <summary>
		/// Gets or sets the current active password for identity verification.
		/// </summary>
		[Required]
		public string CurrentPassword { get; set; }
	}

	/// <summary>
	/// Data transfer object carrying the OTP and new password string to complete an in-app password update.
	/// </summary>
	public class UpdatePasswordDto
	{
		/// <summary>
		/// Gets or sets the 6-digit verification OTP code.
		/// </summary>
		[Required]
		public string Otp { get; set; }

		/// <summary>
		/// Gets or sets the new password string.
		/// </summary>
		[Required]
		[MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
		public string NewPassword { get; set; }
	}

	/// <summary>
	/// Response data transfer object returned upon successful login authentication.
	/// Contains cryptographic JWT bearer token, refresh token, user identity details, and role metadata.
	/// </summary>
	public class AuthResponseDto
	{
		/// <summary>
		/// Gets or sets the signed JWT bearer token string.
		/// </summary>
		public string Token { get; set; }

		/// <summary>
		/// Gets or sets the refresh token string for token renewal.
		/// </summary>
		public string RefreshToken { get; set; }

		/// <summary>
		/// Gets or sets the unique primary key identifier of the user account.
		/// </summary>
		public Guid UserId { get; set; }

		/// <summary>
		/// Gets or sets the primary email address of the authenticated user.
		/// </summary>
		public string Email { get; set; }

		/// <summary>
		/// Gets or sets the role portal string of the user (Patient, Doctor, Admin, SuperAdmin).
		/// </summary>
		public string Role { get; set; }

		/// <summary>
		/// Gets or sets the first name of the authenticated user.
		/// </summary>
		public string FirstName { get; set; }

		/// <summary>
		/// Gets or sets the last name of the authenticated user.
		/// </summary>
		public string LastName { get; set; }

		/// <summary>
		/// Gets or sets the role-specific profile ID (PatientId, DoctorId, or AdminId).
		/// </summary>
		public Guid? ProfileId { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the user must change their temporary password immediately.
		/// </summary>
		public bool RequiresPasswordChange { get; set; }
	}
}
