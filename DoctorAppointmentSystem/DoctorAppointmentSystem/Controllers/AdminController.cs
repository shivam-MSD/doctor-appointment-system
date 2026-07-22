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
				return BadRequest("Missing status query parameter. Values: Verified, Pending, Rejected.");
			}

			var name = await _adminService.VerifyDoctorAsync(doctorId, status);
			if (status.Equals("Verified", StringComparison.OrdinalIgnoreCase))
			{
				return Ok(new { Message = $"Doctor '{name}' approved successfully." });
			}
			else if (status.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
			{
				return Ok(new { Message = $"Doctor '{name}' rejected successfully." });
			}
			return Ok(new { Message = $"Doctor '{name}' verification status updated to '{status}' successfully." });
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

		[HttpGet("system-audit-logs")]
		public async Task<IActionResult> GetSystemAuditLogs(
			[FromQuery] string? entityType,
			[FromQuery] string? action,
			[FromQuery] DateTime? startDate,
			[FromQuery] DateTime? endDate,
			[FromQuery] int page = 1,
			[FromQuery] int size = 10)
		{
			var result = await _adminService.GetSystemAuditLogsAsync(entityType, action, startDate, endDate, page, size);
			return Ok(result);
		}

		[HttpPost("{adminId:guid}/clinics")]
		public async Task<IActionResult> SetClinics(Guid adminId, [FromBody] IEnumerable<Guid> clinicIds)
		{
			var clinics = await _adminService.AssignAdminToClinicsAsync(adminId, clinicIds);
			return Ok(clinics);
		}

		[HttpGet("{adminId:guid}/clinics")]
		public async Task<IActionResult> GetAdminClinics(Guid adminId)
		{
			var clinics = await _adminService.GetClinicsForAdminAsync(adminId);
			return Ok(clinics);
		}
	}
}
