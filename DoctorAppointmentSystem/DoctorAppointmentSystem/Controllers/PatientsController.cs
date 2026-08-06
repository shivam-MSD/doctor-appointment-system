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
		[AllowAnonymous]
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

		[HttpGet("doctors/{doctorId:guid}")]
		[AllowAnonymous]
		public async Task<IActionResult> GetDoctorDetails(Guid doctorId)
		{
			var result = await _patientService.GetDoctorDetailsForPatientAsync(doctorId);
			return Ok(result);
		}

		[HttpGet("cities")]
		[AllowAnonymous]
		public IActionResult GetTopCities()
		{
			var cities = new[]
			{
				new { Name = "All Cities", Code = "" },
				new { Name = "Mumbai", Code = "Mumbai" },
				new { Name = "Delhi / NCR", Code = "Delhi" },
				new { Name = "Bangalore", Code = "Bangalore" },
				new { Name = "Ahmedabad", Code = "Ahmedabad" },
				new { Name = "Pune", Code = "Pune" },
				new { Name = "Hyderabad", Code = "Hyderabad" },
				new { Name = "Chennai", Code = "Chennai" },
				new { Name = "Kolkata", Code = "Kolkata" }
			};
			return Ok(cities);
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

		[HttpGet("family")]
		public async Task<IActionResult> GetFamilyMembers([FromHeader(Name = "X-User-Id")] Guid userId)
		{
			if (userId == Guid.Empty) return BadRequest("Missing required X-User-Id header.");
			var result = await _patientService.GetFamilyMembersAsync(userId);
			return Ok(result);
		}

		[HttpPost("family/dependent")]
		public async Task<IActionResult> CreateDependent([FromHeader(Name = "X-User-Id")] Guid userId, [FromBody] CreateDependentDto dto)
		{
			if (userId == Guid.Empty) return BadRequest("Missing required X-User-Id header.");
			var result = await _patientService.CreateDependentFamilyMemberAsync(userId, dto);
			return Ok(result);
		}

		[HttpPost("family/send-otp")]
		public async Task<IActionResult> SendFamilyOtp([FromHeader(Name = "X-User-Id")] Guid userId, [FromBody] SendFamilyLinkOtpDto dto)
		{
			if (userId == Guid.Empty) return BadRequest("Missing required X-User-Id header.");
			var result = await _patientService.SendFamilyLinkOtpAsync(userId, dto);
			return Ok(result);
		}

		[HttpPost("family/verify-otp")]
		public async Task<IActionResult> VerifyFamilyOtp([FromHeader(Name = "X-User-Id")] Guid userId, [FromBody] VerifyFamilyLinkOtpDto dto)
		{
			if (userId == Guid.Empty) return BadRequest("Missing required X-User-Id header.");
			var result = await _patientService.VerifyFamilyLinkOtpAsync(userId, dto);
			return Ok(result);
		}

		[HttpDelete("family/{familyPatientId:guid}")]
		public async Task<IActionResult> DeleteFamilyMember([FromHeader(Name = "X-User-Id")] Guid userId, Guid familyPatientId)
		{
			if (userId == Guid.Empty) return BadRequest("Missing required X-User-Id header.");
			await _patientService.DeleteFamilyMemberAsync(userId, familyPatientId);
			return Ok(new { Message = "Family member removed successfully." });
		}

		[HttpPost("initiate-contact-update")]
		public async Task<IActionResult> InitiateContactUpdate([FromHeader(Name = "X-User-Id")] Guid userId, [FromBody] InitiateContactUpdateDto dto)
		{
			if (userId == Guid.Empty) return BadRequest("Missing required X-User-Id header.");
			var result = await _patientService.InitiateUpdateContactInfoAsync(userId, dto);
			return Ok(result);
		}

		[HttpPost("confirm-contact-update")]
		public async Task<IActionResult> ConfirmContactUpdate([FromHeader(Name = "X-User-Id")] Guid userId, [FromBody] ConfirmContactUpdateDto dto)
		{
			if (userId == Guid.Empty) return BadRequest("Missing required X-User-Id header.");
			var result = await _patientService.ConfirmUpdateContactInfoAsync(userId, dto);
			return Ok(result);
		}
	}
}
