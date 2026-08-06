using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DoctorAppointmentSystem.Application.DTOs;
using DoctorAppointmentSystem.Application.Services;

namespace DoctorAppointmentSystem.Controllers
{
	/// <summary>
	/// API Controller managing authentication endpoints: registration, login, OTP email verification, and password recovery.
	/// </summary>
	[ApiController]
	[Route("api/[controller]")]
	public class AuthController : ControllerBase
	{
		private readonly IAuthService _authService;

		/// <summary>
		/// Initializes a new instance of the <see cref="AuthController"/> class.
		/// </summary>
		/// <param name="authService">Authentication service instance.</param>
		public AuthController(IAuthService authService)
		{
			_authService = authService;
		}

		/// <summary>
		/// Registers a new patient account and queues an email verification OTP via Hangfire.
		/// </summary>
		/// <param name="registerDto">Patient sign-up details.</param>
		/// <returns>AuthResponseDto with registration status.</returns>
		[HttpPost("register")]
		public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
		{
			var response = await _authService.RegisterAsync(registerDto);
			return Ok(response);
		}

		/// <summary>
		/// Submits a medical doctor onboarding application for administrative verification.
		/// </summary>
		/// <param name="doctorSignUpDto">Doctor onboarding application payload.</param>
		/// <returns>AuthResponseDto metadata.</returns>
		[HttpPost("register-doctor")]
		public async Task<IActionResult> RegisterDoctor([FromBody] DoctorSignUpDto doctorSignUpDto)
		{
			var response = await _authService.RegisterDoctorAsync(doctorSignUpDto);
			return Ok(response);
		}

		/// <summary>
		/// Authenticates user credentials, generates a cryptographic JWT token, dispatches a Login Security Email Alert via Hangfire, and records a Login Audit Log.
		/// </summary>
		/// <param name="loginDto">User login credentials.</param>
		/// <returns>AuthResponseDto with JWT token and profile info.</returns>
		[HttpPost("login")]
		public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
		{
			var response = await _authService.LoginAsync(loginDto);
			return Ok(response);
		}

		/// <summary>
		/// Verifies a user's email address using a 6-digit OTP code to complete account activation.
		/// </summary>
		/// <param name="dto">OTP verification payload.</param>
		/// <returns>AuthResponseDto metadata.</returns>
		[HttpPost("verify-email")]
		public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailDto dto)
		{
			var response = await _authService.VerifyEmailAsync(dto);
			return Ok(response);
		}

		/// <summary>
		/// Checks if an email address exists and returns its registered role portal.
		/// </summary>
		/// <param name="dto">Email check payload.</param>
		/// <returns>Role portal status object.</returns>
		[HttpPost("check-email")]
		public async Task<IActionResult> CheckEmail([FromBody] CheckEmailDto dto)
		{
			var role = await _authService.CheckEmailRoleAsync(dto.Email);
			if (role == null)
			{
				return NotFound(new { detail = "No account found with this email address." });
			}
			if (!string.IsNullOrEmpty(dto.Role) && !string.Equals(role, dto.Role, StringComparison.OrdinalIgnoreCase))
			{
				return BadRequest(new { detail = $"This email is registered under the role '{role}', not '{dto.Role}'." });
			}
			return Ok(new { exists = true, role });
		}

		/// <summary>
		/// Initiates a forgot password workflow and queues an OTP email via Hangfire.
		/// </summary>
		/// <param name="dto">Forgot password request payload.</param>
		/// <returns>Success message object.</returns>
		[HttpPost("forgot-password")]
		public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
		{
			await _authService.ForgotPasswordAsync(dto);
			return Ok(new { message = "OTP sent to your email address. Please check your inbox." });
		}

		/// <summary>
		/// Resets a user password using a valid 6-digit OTP code.
		/// </summary>
		/// <param name="dto">Reset password payload.</param>
		/// <returns>Success message object.</returns>
		[HttpPost("reset-password")]
		public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
		{
			await _authService.ResetPasswordAsync(dto);
			return Ok(new { message = "Password reset successfully. You can now log in with your new password." });
		}

		/// <summary>
		/// Initiates an in-app password update for a logged-in user after current password verification.
		/// </summary>
		/// <param name="dto">Current password payload.</param>
		/// <returns>Success message object.</returns>
		[Authorize]
		[HttpPost("initiate-password-update")]
		public async Task<IActionResult> InitiatePasswordUpdate([FromBody] InitiatePasswordUpdateDto dto)
		{
			var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
			await _authService.InitiatePasswordUpdateAsync(userId, dto);
			return Ok(new { message = "Current password verified. An OTP has been sent to your registered email." });
		}

		/// <summary>
		/// Completes an in-app password update using the 6-digit OTP code.
		/// </summary>
		/// <param name="dto">Update password payload with OTP and new password.</param>
		/// <returns>Success message object.</returns>
		[Authorize]
		[HttpPost("update-password")]
		public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordDto dto)
		{
			var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
			await _authService.UpdatePasswordAsync(userId, dto);
			return Ok(new { message = "Password updated successfully." });
		}

		/// <summary>
		/// Dispatches an OTP code via Email or WhatsApp.
		/// </summary>
		[HttpPost("send-otp")]
		public async Task<IActionResult> SendAuthOtp([FromBody] SendAuthOtpDto dto)
		{
			System.Diagnostics.Debugger.Launch();
			await _authService.SendAuthOtpAsync(dto);
			return Ok(new { message = $"OTP dispatched successfully via {dto.Channel}!" });
		}

		/// <summary>
		/// Verifies an OTP code sent via Email or WhatsApp.
		/// </summary>
		[HttpPost("verify-otp")]
		public async Task<IActionResult> VerifyAuthOtp([FromBody] VerifyAuthOtpDto dto)
		{
			var isValid = await _authService.VerifyAuthOtpAsync(dto);
			if (!isValid)
			{
				return BadRequest(new { detail = "The OTP code entered is invalid or has expired." });
			}
			return Ok(new { valid = true, message = "OTP verified successfully!" });
		}

		/// <summary>
		/// Passwordless login via WhatsApp mobile number + OTP.
		/// </summary>
		[HttpPost("login-whatsapp")]
		public async Task<IActionResult> LoginWithWhatsApp([FromBody] WhatsAppLoginDto dto)
		{
			var response = await _authService.LoginWithWhatsAppOtpAsync(dto);
			return Ok(response);
		}

		/// <summary>
		/// Verifies a 2FA OTP code and completes authentication.
		/// </summary>
		[HttpPost("verify-2fa")]
		public async Task<IActionResult> VerifyTwoFactor([FromBody] VerifyTwoFactorDto dto)
		{
			var response = await _authService.VerifyTwoFactorAsync(dto);
			return Ok(response);
		}
	}
}
