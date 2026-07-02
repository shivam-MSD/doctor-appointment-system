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
	}
}
