using System;
using System.Threading.Tasks;
using DoctorAppointmentSystem.Application.DTOs;

namespace DoctorAppointmentSystem.Application.Services
{
	/// <summary>
	/// Service interface defining authentication, user registration, OTP email verification, and password management contracts.
	/// </summary>
	public interface IAuthService
	{
		/// <summary>Event delegate triggered when a background email dispatch is requested.</summary>
		event EmailSendEventHandler? EmailSendEvent;

		/// <summary>Handles background email dispatch event invocations.</summary>
		void OnEmailSendHandle(object o, EmailSendEventArgs emailSendEvent);

		/// <summary>
		/// Registers a new patient account, generates an email verification OTP, and queues the OTP verification email via Hangfire.
		/// </summary>
		/// <param name="registerDto">Patient registration sign-up payload.</param>
		/// <returns>AuthResponseDto with JWT token and profile details upon verification.</returns>
		Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto);

		/// <summary>
		/// Registers a new medical doctor onboarding application for administrative review by Super Admin.
		/// </summary>
		/// <param name="doctorSignUpDto">Doctor onboarding application payload.</param>
		/// <returns>AuthResponseDto metadata.</returns>
		Task<AuthResponseDto> RegisterDoctorAsync(DoctorSignUpDto doctorSignUpDto);

		/// <summary>
		/// Authenticates user login credentials, generates a cryptographic JWT token, queues a Login Security Email Alert via Hangfire, and records a Login Audit Log.
		/// </summary>
		/// <param name="loginDto">Login credentials payload (Email, Password, Role).</param>
		/// <returns>AuthResponseDto containing JWT token and user profile metadata.</returns>
		Task<AuthResponseDto> LoginAsync(LoginDto loginDto);

		/// <summary>
		/// Verifies a user's email address using a 6-digit OTP code to complete account activation.
		/// </summary>
		/// <param name="dto">OTP verification payload (Email, OTP).</param>
		/// <returns>AuthResponseDto upon successful activation.</returns>
		Task<AuthResponseDto> VerifyEmailAsync(VerifyEmailDto dto);

		/// <summary>
		/// Checks whether an email address exists in the system and returns its registered role portal name.
		/// </summary>
		/// <param name="email">Email address to check.</param>
		/// <returns>Role name string (e.g. "Doctor", "Patient", "Admin", "SuperAdmin") or null.</returns>
		Task<string?> CheckEmailRoleAsync(string email);

		/// <summary>
		/// Initiates a forgot password workflow, generating a 6-digit OTP code and sending it via Hangfire.
		/// </summary>
		/// <param name="dto">Forgot password payload containing email address and role.</param>
		Task ForgotPasswordAsync(ForgotPasswordDto dto);

		/// <summary>
		/// Resets a user's password using a valid OTP code.
		/// </summary>
		/// <param name="dto">Reset password payload containing Email, OTP, and NewPassword.</param>
		Task ResetPasswordAsync(ResetPasswordDto dto);

		/// <summary>
		/// Initiates an in-app password update for a logged-in user after verifying their current password.
		/// </summary>
		/// <param name="userId">Authenticated user ID.</param>
		/// <param name="dto">Current password payload.</param>
		Task InitiatePasswordUpdateAsync(Guid userId, InitiatePasswordUpdateDto dto);

		/// <summary>
		/// Completes an in-app password update using the 6-digit OTP code.
		/// </summary>
		/// <param name="userId">Authenticated user ID.</param>
		/// <param name="dto">Update password payload containing OTP and NewPassword.</param>
		Task UpdatePasswordAsync(Guid userId, UpdatePasswordDto dto);

		/// <summary>
		/// Dispatches an OTP via Email or WhatsApp for registration, login, or profile updates.
		/// </summary>
		Task SendAuthOtpAsync(SendAuthOtpDto dto);

		/// <summary>
		/// Verifies an OTP code sent via Email or WhatsApp.
		/// </summary>
		Task<bool> VerifyAuthOtpAsync(VerifyAuthOtpDto dto);

		/// <summary>
		/// Authenticates user passwordlessly via WhatsApp mobile number and valid OTP.
		/// </summary>
		Task<AuthResponseDto> LoginWithWhatsAppOtpAsync(WhatsAppLoginDto dto);

		/// <summary>
		/// Completes 2FA login verification using 6-digit OTP code.
		/// </summary>
		Task<AuthResponseDto> VerifyTwoFactorAsync(VerifyTwoFactorDto dto);
	}
}
