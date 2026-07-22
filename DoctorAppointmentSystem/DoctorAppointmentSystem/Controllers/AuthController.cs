using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using DoctorAppointmentSystem.Application.DTOs;
using DoctorAppointmentSystem.Application.Services;

namespace DoctorAppointmentSystem.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class AuthController : ControllerBase
	{
		private readonly IAuthService _authService;

		public AuthController(IAuthService authService)
		{
			_authService = authService;
		}

		[HttpPost("register")]
		public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
		{
			var response = await _authService.RegisterAsync(registerDto);
			return Ok(response);
		}

		[HttpPost("register-doctor")]
		public async Task<IActionResult> RegisterDoctor([FromBody] DoctorSignUpDto doctorSignUpDto)
		{
			var response = await _authService.RegisterDoctorAsync(doctorSignUpDto);
			return Ok(response);
		}

		[HttpPost("login")]
		public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
		{
			var response = await _authService.LoginAsync(loginDto);
			return Ok(response);
		}

		[HttpPost("verify-email")]
		public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailDto dto)
		{
			var response = await _authService.VerifyEmailAsync(dto);
			return Ok(response);
		}

		[HttpPost("check-email")]
		public async Task<IActionResult> CheckEmail([FromBody] CheckEmailDto dto)
		{
			var role = await _authService.CheckEmailRoleAsync(dto.Email);
			if (role == null)
			{
				return NotFound(new { detail = "No account found with this email address." });
			}
			return Ok(new { exists = true, role });
		}

		[HttpPost("forgot-password")]
		public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
		{
			await _authService.ForgotPasswordAsync(dto);
			return Ok(new { message = "OTP sent to your email address. Please check your inbox." });
		}

		[HttpPost("reset-password")]
		public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
		{
			await _authService.ResetPasswordAsync(dto);
			return Ok(new { message = "Password reset successfully. You can now log in with your new password." });
		}

		[Authorize]
		[HttpPost("initiate-password-update")]
		public async Task<IActionResult> InitiatePasswordUpdate([FromBody] InitiatePasswordUpdateDto dto)
		{
			var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
			await _authService.InitiatePasswordUpdateAsync(userId, dto);
			return Ok(new { message = "Current password verified. An OTP has been sent to your registered email." });
		}

		[Authorize]
		[HttpPost("update-password")]
		public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordDto dto)
		{
			var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
			await _authService.UpdatePasswordAsync(userId, dto);
			return Ok(new { message = "Password updated successfully." });
		}
	}
}

