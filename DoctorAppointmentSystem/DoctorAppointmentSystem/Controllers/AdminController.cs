using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DoctorAppointmentSystem.Application.Services;

namespace DoctorAppointmentSystem.Controllers
{
	/// <summary>
	/// API Controller managing Super Admin administrative operations: doctor onboarding verification, system audits, and multi-clinic admin assignments.
	/// </summary>
	[ApiController]
	[Route("api/[controller]")]
	[Authorize(Roles = "SuperAdmin")]
	public class AdminController : ControllerBase
	{
		private readonly IAdminService _adminService;

		/// <summary>
		/// Initializes a new instance of the <see cref="AdminController"/> class.
		/// </summary>
		/// <param name="adminService">Super Admin service instance.</param>
		public AdminController(IAdminService adminService)
		{
			_adminService = adminService;
		}

		/// <summary>
		/// Approves or rejects a doctor onboarding application and generates temporary login credentials upon approval.
		/// </summary>
		/// <param name="doctorId">Target doctor ID.</param>
		/// <param name="status">Verification status (Verified, Pending, Rejected).</param>
		/// <param name="rejectionReason">Optional rejection explanation.</param>
		/// <returns>Status confirmation message.</returns>
		[HttpPost("verify-doctor/{doctorId:guid}")]
		public async Task<IActionResult> VerifyDoctor(Guid doctorId, [FromQuery] string status, [FromQuery] string? rejectionReason = null)
		{
			if (string.IsNullOrEmpty(status))
			{
				return BadRequest("Missing status query parameter. Values: Verified, Pending, Rejected.");
			}

			var name = await _adminService.VerifyDoctorAsync(doctorId, status, rejectionReason);
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

		/// <summary>
		/// Retrieves all pending doctor onboarding applications awaiting Super Admin review.
		/// </summary>
		/// <returns>List of pending doctor profiles.</returns>
		[HttpGet("pending-doctors")]
		public async Task<IActionResult> GetPendingDoctors()
		{
			var result = await _adminService.GetPendingDoctorsAsync();
			return Ok(result);
		}

		/// <summary>
		/// Retrieves all doctors registered in the network with optional search and date filters.
		/// </summary>
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

		/// <summary>
		/// Retrieves all clinic branches in the network with location and verification status filters.
		/// </summary>
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

		/// <summary>
		/// Retrieves all clinic administrators in the network.
		/// </summary>
		[HttpGet("admins")]
		public async Task<IActionResult> GetAllAdmins(
			[FromQuery] string? search,
			[FromQuery] bool? isVerified)
		{
			var result = await _adminService.GetAllAdminsAsync(search, isVerified);
			return Ok(result);
		}

		/// <summary>
		/// Retrieves paginated system-wide audit logs for security oversight.
		/// </summary>
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

		/// <summary>
		/// Assigns a Clinic Administrator to manage one or multiple clinic branches.
		/// </summary>
		[HttpPost("{adminId:guid}/clinics")]
		public async Task<IActionResult> SetClinics(Guid adminId, [FromBody] IEnumerable<Guid> clinicIds)
		{
			var clinics = await _adminService.AssignAdminToClinicsAsync(adminId, clinicIds);
			return Ok(clinics);
		}

		/// <summary>
		/// Retrieves all clinic branches currently managed by a specific Clinic Administrator.
		/// </summary>
		[HttpGet("{adminId:guid}/clinics")]
		public async Task<IActionResult> GetAdminClinics(Guid adminId)
		{
			var clinics = await _adminService.GetClinicsForAdminAsync(adminId);
			return Ok(clinics);
		}
	}
}
