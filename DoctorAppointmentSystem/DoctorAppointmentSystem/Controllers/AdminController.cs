using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DoctorAppointmentSystem.Application.Services;

namespace DoctorAppointmentSystem.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	[Authorize(Roles = "SuperAdmin")]
	public class AdminController : ControllerBase
	{
		private readonly IAdminService _adminService;

		public AdminController(IAdminService adminService)
		{
			_adminService = adminService;
		}

		[HttpPost("verify-doctor/{doctorId:guid}")]
		public async Task<IActionResult> VerifyDoctor(Guid doctorId, [FromQuery] string status)
		{
			if (string.IsNullOrEmpty(status))
			{
				return BadRequest("Missing status query parameter. Values: Verified, Pending.");
			}

			await _adminService.VerifyDoctorAsync(doctorId, status);
			return Ok(new { Message = $"Doctor verification status updated to '{status}' successfully." });
		}

		[HttpGet("pending-doctors")]
		public async Task<IActionResult> GetPendingDoctors()
		{
			var result = await _adminService.GetPendingDoctorsAsync();
			return Ok(result);
		}

		[HttpGet("doctors")]
		public async Task<IActionResult> GetAllDoctors(
			[FromQuery] string? search,
			[FromQuery] string? status,
			[FromQuery] DateTime? registerDate,
			[FromQuery] DateTime? approveDate)
		{
			var result = await _adminService.GetAllDoctorsAsync(search, status, registerDate, approveDate);
			return Ok(result);
		}

		[HttpGet("clinics")]
		public async Task<IActionResult> GetAllClinics(
			[FromQuery] string? search,
			[FromQuery] string? state,
			[FromQuery] string? city,
			[FromQuery] bool? isVerified)
		{
			var result = await _adminService.GetAllClinicsAsync(search, state, city, isVerified);
			return Ok(result);
		}

		[HttpGet("admins")]
		public async Task<IActionResult> GetAllAdmins(
			[FromQuery] string? search,
			[FromQuery] bool? isVerified)
		{
			var result = await _adminService.GetAllAdminsAsync(search, isVerified);
			return Ok(result);
		}
	}
}
