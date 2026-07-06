using Microsoft.AspNetCore.Mvc;
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
	}
}
