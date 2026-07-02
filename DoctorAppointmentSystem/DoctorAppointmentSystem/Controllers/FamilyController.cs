using Microsoft.AspNetCore.Mvc;
using DoctorAppointmentSystem.Application.DTOs;
using DoctorAppointmentSystem.Application.Services;

namespace DoctorAppointmentSystem.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class FamilyController : ControllerBase
	{
		private readonly IFamilyService _familyService;

		public FamilyController(IFamilyService familyService)
		{
			_familyService = familyService;
		}

		[HttpPost("add")]
		public async Task<IActionResult> InitiateAdd([FromHeader(Name = "X-User-Id")] Guid userId, [FromBody] AddFamilyMemberDto dto)
		{
			if (userId == Guid.Empty)
			{
				return BadRequest("Missing required X-User-Id header representing the authenticated user.");
			}

			var verificationId = await _familyService.InitiateAddFamilyMemberAsync(userId, dto);
			return Ok(new { VerificationId = verificationId, Message = "Verification OTP has been generated." });
		}

		[HttpPost("verify")]
		public async Task<IActionResult> Verify([FromHeader(Name = "X-User-Id")] Guid userId, [FromBody] VerifyFamilyOtpDto dto)
		{
			if (userId == Guid.Empty)
			{
				return BadRequest("Missing required X-User-Id header representing the authenticated user.");
			}

			var patient = await _familyService.VerifyFamilyMemberOtpAsync(userId, dto);
			return Ok(patient);
		}

		[HttpGet]
		public async Task<IActionResult> GetFamily([FromHeader(Name = "X-User-Id")] Guid userId)
		{
			if (userId == Guid.Empty)
			{
				return BadRequest("Missing required X-User-Id header representing the authenticated user.");
			}

			var family = await _familyService.GetFamilyMembersAsync(userId);
			return Ok(family);
		}
	}
}
