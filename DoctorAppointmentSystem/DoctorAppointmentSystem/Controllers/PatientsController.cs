using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DoctorAppointmentSystem.Application.DTOs;
using DoctorAppointmentSystem.Application.Services;

namespace DoctorAppointmentSystem.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	[Authorize]
	public class PatientsController : ControllerBase
	{
		private readonly IPatientService _patientService;

		public PatientsController(IPatientService patientService)
		{
			_patientService = patientService;
		}

		[HttpGet("doctors")]
		public async Task<IActionResult> GetDoctors(
			[FromQuery] string? search,
			[FromQuery] Guid? specializationId,
			[FromQuery] string? state,
			[FromQuery] string? city,
			[FromQuery] int page = 1,
			[FromQuery] int size = 10)
		{
			var result = await _patientService.GetDoctorsForPatientAsync(search, specializationId, state, city, page, size);
			return Ok(result);
		}

		[HttpGet("{id:guid}")]
		public async Task<IActionResult> GetProfile([FromHeader(Name = "X-User-Id")] Guid userId, Guid id)
		{
			if (userId == Guid.Empty)
			{
				return BadRequest("Missing required X-User-Id header representing the authenticated user.");
			}

			var patient = await _patientService.GetPatientProfileAsync(userId, id);
			return Ok(patient);
		}

		[HttpPut("{id:guid}")]
		public async Task<IActionResult> UpdateProfile([FromHeader(Name = "X-User-Id")] Guid userId, Guid id, [FromBody] PatientUpdateDto dto)
		{
			if (userId == Guid.Empty)
			{
				return BadRequest("Missing required X-User-Id header representing the authenticated user.");
			}

			var patient = await _patientService.UpdatePatientProfileAsync(userId, id, dto);
			return Ok(patient);
		}
	}
}
