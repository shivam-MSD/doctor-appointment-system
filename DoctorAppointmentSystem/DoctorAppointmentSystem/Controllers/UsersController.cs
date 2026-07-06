using Microsoft.AspNetCore.Mvc;
using DoctorAppointmentSystem.Application.DTOs;
using DoctorAppointmentSystem.Application.Services;

namespace DoctorAppointmentSystem.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class UsersController : ControllerBase
	{
		private readonly IUserService _userService;

		public UsersController(IUserService userService)
		{
			_userService = userService;
		}

		[HttpGet("profile")]
		public async Task<IActionResult> GetProfile([FromHeader(Name = "X-User-Id")] Guid userId)
		{
			if (userId == Guid.Empty)
			{
				return BadRequest("Missing required X-User-Id header representing the authenticated user.");
			}

			var user = await _userService.GetUserProfileAsync(userId);
			return Ok(user);
		}

		[HttpPost("change-password")]
		public async Task<IActionResult> ChangePassword([FromHeader(Name = "X-User-Id")] Guid userId, [FromBody] ChangePasswordDto dto)
		{
			if (userId == Guid.Empty)
			{
				return BadRequest("Missing required X-User-Id header representing the authenticated user.");
			}

			await _userService.ChangePasswordAsync(userId, dto);
			return Ok(new { Message = "Password changed successfully." });
		}

		[HttpGet("doctor-profile")]
		public async Task<IActionResult> GetDoctorProfile([FromHeader(Name = "X-User-Id")] Guid userId)
		{
			if (userId == Guid.Empty)
			{
				return BadRequest("Missing required X-User-Id header.");
			}

			var result = await _userService.GetDoctorProfileAsync(userId);
			return Ok(result);
		}

		[HttpPut("doctor-profile")]
		public async Task<IActionResult> UpdateDoctorProfile([FromHeader(Name = "X-User-Id")] Guid userId, [FromBody] DoctorProfileDto dto)
		{
			if (userId == Guid.Empty)
			{
				return BadRequest("Missing required X-User-Id header.");
			}

			var result = await _userService.UpdateDoctorProfileAsync(userId, dto);
			return Ok(result);
		}

		[HttpGet("admin-profile")]
		public async Task<IActionResult> GetAdminProfile([FromHeader(Name = "X-User-Id")] Guid userId)
		{
			if (userId == Guid.Empty)
			{
				return BadRequest("Missing required X-User-Id header.");
			}

			var result = await _userService.GetAdminProfileAsync(userId);
			return Ok(result);
		}

		[HttpPut("admin-profile")]
		public async Task<IActionResult> UpdateAdminProfile([FromHeader(Name = "X-User-Id")] Guid userId, [FromBody] AdminProfileDto dto)
		{
			if (userId == Guid.Empty)
			{
				return BadRequest("Missing required X-User-Id header.");
			}

			var result = await _userService.UpdateAdminProfileAsync(userId, dto);
			return Ok(result);
		}
	}
}
