using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DoctorAppointmentSystem.Application.DTOs;
using DoctorAppointmentSystem.Application.Services;

namespace DoctorAppointmentSystem.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	[Authorize]
	public class ClinicsController : ControllerBase
	{
		private readonly IClinicService _clinicService;

		public ClinicsController(IClinicService clinicService)
		{
			_clinicService = clinicService;
		}

		[HttpPost("register-only")]
		public async Task<IActionResult> RegisterClinicOnly(
			[FromHeader(Name = "X-User-Id")] Guid doctorUserId,
			[FromBody] CreateClinicDto dto)
		{
			if (doctorUserId == Guid.Empty)
			{
				return BadRequest("Missing required X-User-Id header.");
			}
			await _clinicService.RegisterClinicAsync(doctorUserId, dto);
			return Ok(new { Message = "Clinic registered successfully. Pending Super Admin verification." });
		}

		[HttpPost("register-admin")]
		public async Task<IActionResult> RegisterAdminForClinic(
			[FromHeader(Name = "X-User-Id")] Guid doctorUserId,
			[FromBody] RegisterAdminForClinicDto dto)
		{
			if (doctorUserId == Guid.Empty)
			{
				return BadRequest("Missing required X-User-Id header.");
			}
			await _clinicService.RegisterAdminForClinicAsync(doctorUserId, dto);
			return Ok(new { Message = "Clinic Admin registered successfully. Pending Super Admin verification." });
		}

		[HttpPost("register")]
		public async Task<IActionResult> RegisterClinic(
			[FromHeader(Name = "X-User-Id")] Guid doctorUserId,
			[FromBody] RegisterClinicDto dto)
		{
			if (doctorUserId == Guid.Empty)
			{
				return BadRequest("Missing required X-User-Id header representing the authenticated doctor.");
			}

			await _clinicService.RegisterClinicAndAdminAsync(doctorUserId, dto);
			return Ok(new { Message = "Clinic and Clinic Admin registered successfully. Pending Super Admin verification." });
		}

		[HttpGet]
		public async Task<IActionResult> GetDoctorClinics([FromHeader(Name = "X-User-Id")] Guid doctorUserId)
		{
			if (doctorUserId == Guid.Empty)
			{
				return BadRequest("Missing required X-User-Id header.");
			}

			var result = await _clinicService.GetDoctorClinicsAsync(doctorUserId);
			return Ok(result);
		}

		[HttpGet("doctor/{doctorId:guid}")]
		public async Task<IActionResult> GetClinicsByDoctorId(Guid doctorId)
		{
			System.Diagnostics.Debugger.Launch();
			var result = await _clinicService.GetClinicsByDoctorIdAsync(doctorId);
			return Ok(result);
		}

		[HttpGet("admins")]
		public async Task<IActionResult> GetDoctorAdmins([FromHeader(Name = "X-User-Id")] Guid doctorUserId)
		{
			if (doctorUserId == Guid.Empty)
			{
				return BadRequest("Missing required X-User-Id header.");
			}

			var result = await _clinicService.GetDoctorAdminsAsync(doctorUserId);
			return Ok(result);
		}

		[HttpGet("pending")]
		public async Task<IActionResult> GetPendingClinics()
		{
			var result = await _clinicService.GetPendingClinicsAsync();
			return Ok(result);
		}

		[HttpGet("pending-admins")]
		public async Task<IActionResult> GetPendingAdmins()
		{
			var result = await _clinicService.GetPendingAdminsAsync();
			return Ok(result);
		}

		[HttpPost("verify-clinic/{clinicId}")]
		public async Task<IActionResult> VerifyClinic(Guid clinicId)
		{
			var name = await _clinicService.VerifyClinicAsync(clinicId);
			return Ok(new { Message = $"Clinic '{name}' verified successfully." });
		}

		[HttpPost("verify-clinic/{clinicId}/reject")]
		public async Task<IActionResult> RejectClinic(Guid clinicId, [FromBody] RejectClinicDto dto)
		{
			var name = await _clinicService.RejectClinicAsync(clinicId, dto.RejectionReason);
			return Ok(new { Message = $"Clinic '{name}' has been rejected successfully." });
		}

		[HttpPut("{clinicId}")]
		public async Task<IActionResult> UpdateClinic(
			Guid clinicId,
			[FromHeader(Name = "X-User-Id")] Guid doctorUserId,
			[FromBody] UpdateClinicDto dto)
		{
			if (doctorUserId == Guid.Empty)
			{
				return BadRequest("Missing required X-User-Id header representing the authenticated doctor.");
			}
			await _clinicService.UpdateClinicAsync(clinicId, doctorUserId, dto);
			return Ok(new { Message = "Clinic details updated successfully. Pending Super Admin verification." });
		}

		[HttpPut("admin-update")]
		public async Task<IActionResult> AdminUpdateClinic(
			[FromHeader(Name = "X-User-Id")] Guid adminUserId,
			[FromBody] UpdateClinicDto dto)
		{
			if (adminUserId == Guid.Empty)
			{
				return BadRequest("Missing required X-User-Id header representing the authenticated admin.");
			}
			await _clinicService.AdminUpdateClinicAsync(adminUserId, dto);
			return Ok(new { Message = "Clinic details updated successfully." });
		}

		[HttpGet("my-clinic")]
		public async Task<IActionResult> GetMyClinic([FromHeader(Name = "X-User-Id")] Guid adminUserId)
		{
			if (adminUserId == Guid.Empty)
			{
				return BadRequest("Missing required X-User-Id header representing the authenticated admin.");
			}
			var result = await _clinicService.GetAdminClinicAsync(adminUserId);
			return Ok(result);
		}

		[HttpPost("verify-admin/{adminId}")]
		public async Task<IActionResult> VerifyAdmin(Guid adminId)
		{
			var name = await _clinicService.VerifyAdminAsync(adminId);
			return Ok(new { Message = $"Clinic Admin '{name}' verified successfully." });
		}

		[HttpPost("reject-admin/{adminId}")]
		public async Task<IActionResult> RejectAdmin(Guid adminId)
		{
			var name = await _clinicService.RejectAdminAsync(adminId);
			return Ok(new { Message = $"Clinic Admin '{name}' has been rejected successfully." });
		}

		[HttpGet("{clinicId:guid}/history")]
		public async Task<IActionResult> GetClinicHistory(Guid clinicId)
		{
			var result = await _clinicService.GetClinicHistoryAsync(clinicId);
			return Ok(result);
		}
	}
}
