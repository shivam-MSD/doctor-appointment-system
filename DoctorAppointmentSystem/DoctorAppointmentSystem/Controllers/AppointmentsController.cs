using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DoctorAppointmentSystem.Application.DTOs;
using DoctorAppointmentSystem.Application.Services;

namespace DoctorAppointmentSystem.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	[Authorize]
	public class AppointmentsController : ControllerBase
	{
		private readonly IAppointmentService _appointmentService;

		public AppointmentsController(IAppointmentService appointmentService)
		{
			_appointmentService = appointmentService;
		}

		[HttpPost("book")]
		public async Task<IActionResult> BookAppointment([FromHeader(Name = "X-User-Id")] Guid userId, [FromBody] CreateAppointmentDto dto)
		{
			if (userId == Guid.Empty)
			{
				return BadRequest("Missing required X-User-Id header representing the authenticated user.");
			}

			var appointment = await _appointmentService.BookAppointmentAsync(userId, dto);
			return Ok(appointment);
		}

		[HttpPost("cancel/{id:guid}")]
		public async Task<IActionResult> CancelAppointment([FromHeader(Name = "X-User-Id")] Guid userId, Guid id)
		{
			if (userId == Guid.Empty)
			{
				return BadRequest("Missing required X-User-Id header representing the authenticated user.");
			}

			await _appointmentService.CancelAppointmentAsync(userId, id);
			return Ok(new { Message = "Appointment cancelled successfully." });
		}

		[HttpGet("admin-doctor-dashboard")]
		public async Task<IActionResult> GetAdminDoctorDashboard(
			[FromHeader(Name = "X-User-Id")] Guid userId,
			[FromQuery] string? status,
			[FromQuery] DateTime? startDate,
			[FromQuery] DateTime? endDate,
			[FromQuery] string? search,
			[FromQuery] Guid? patientId,
			[FromQuery] int page = 1,
			[FromQuery] int size = 10)
		{
			if (userId == Guid.Empty)
			{
				return BadRequest("Missing required X-User-Id header representing the authenticated user.");
			}

			var result = await _appointmentService.GetAdminDoctorDashboardAppointmentsAsync(userId, status, startDate, endDate, search, patientId, page, size);
			return Ok(result);
		}

		[HttpGet("patient-dashboard")]
		public async Task<IActionResult> GetPatientDashboard(
			[FromHeader(Name = "X-User-Id")] Guid userId,
			[FromQuery] string? status,
			[FromQuery] int page = 1,
			[FromQuery] int size = 10)
		{
			if (userId == Guid.Empty)
			{
				return BadRequest("Missing required X-User-Id header representing the authenticated user.");
			}

			var result = await _appointmentService.GetPatientDashboardAppointmentsAsync(userId, status, page, size);
			return Ok(result);
		}

		[HttpGet("consulted-doctors")]
		public async Task<IActionResult> GetConsultedDoctors([FromHeader(Name = "X-User-Id")] Guid userId)
		{
			if (userId == Guid.Empty)
			{
				return BadRequest("Missing required X-User-Id header representing the authenticated user.");
			}

			var result = await _appointmentService.GetConsultedDoctorsAsync(userId);
			return Ok(result);
		}

		[HttpGet("patients-list")]
		public async Task<IActionResult> GetPatientsList(
			[FromHeader(Name = "X-User-Id")] Guid userId,
			[FromQuery] string? search,
			[FromQuery] int page = 1,
			[FromQuery] int size = 10)
		{
			if (userId == Guid.Empty)
			{
				return BadRequest("Missing required X-User-Id header representing the authenticated user.");
			}

			var result = await _appointmentService.GetDashboardPatientsAsync(userId, search, page, size);
			return Ok(result);
		}

		[HttpGet("available-doctors")]
		public async Task<IActionResult> GetAvailableDoctors()
		{
			var result = await _appointmentService.GetAvailableDoctorsAsync();
			return Ok(result);
		}

		[HttpGet("booking-details")]
		public async Task<IActionResult> GetBookingDetails([FromQuery] Guid doctorId, [FromQuery] Guid clinicId)
		{
			var result = await _appointmentService.GetBookingDetailsAsync(doctorId, clinicId);
			return Ok(result);
		}

		[HttpGet("specializations")]
		[AllowAnonymous]
		public async Task<IActionResult> GetSpecializations()
		{
			var result = await _appointmentService.GetSpecializationsAsync();
			return Ok(result);
		}

		[HttpGet("search-doctors")]
		public async Task<IActionResult> SearchDoctors(
			[FromQuery] string? state,
			[FromQuery] string? city,
			[FromQuery] Guid? specializationId,
			[FromQuery] string? name)
		{
			if (string.IsNullOrWhiteSpace(state) && string.IsNullOrWhiteSpace(city) && (!specializationId.HasValue || specializationId == Guid.Empty) && string.IsNullOrWhiteSpace(name))
			{
				return BadRequest("At least one search filter (Name, Specialization, State, or City) must be provided.");
			}

			var result = await _appointmentService.SearchDoctorsAsync(state, city, specializationId, name);
			return Ok(result);
		}

		[HttpGet("doctors/{doctorId:guid}/clinics")]
		public async Task<IActionResult> GetClinicsForDoctor(Guid doctorId)
		{
			var result = await _appointmentService.GetClinicsByDoctorIdAsync(doctorId);
			return Ok(result);
		}

		[HttpGet("booked-slots")]
		public async Task<IActionResult> GetBookedSlots(
			[FromQuery] Guid doctorId,
			[FromQuery] Guid clinicId,
			[FromQuery] DateTime date,
			[FromQuery] Guid? patientId)
		{
			var result = await _appointmentService.GetBookedSlotsAsync(doctorId, clinicId, date, patientId);
			return Ok(result);
		}

		[HttpPost("approve/{id:guid}")]
		public async Task<IActionResult> ApproveAppointment(Guid id, [FromBody] ApproveAppointmentDto dto)
		{
			await _appointmentService.ApproveAppointmentAsync(id, dto.Comment);
			return Ok(new { Message = "Appointment approved successfully." });
		}

		[HttpPost("reject/{id:guid}")]
		public async Task<IActionResult> RejectAppointment(Guid id, [FromBody] RejectAppointmentDto dto)
		{
			await _appointmentService.RejectAppointmentAsync(id, dto.Reason);
			return Ok(new { Message = "Appointment rejected successfully." });
		}

		[HttpPost("complete/{id:guid}")]
		public async Task<IActionResult> CompleteAppointment(Guid id, [FromBody] CompleteAppointmentDto dto)
		{
			await _appointmentService.CompleteAppointmentAsync(id, dto.Comment, dto.Report);
			return Ok(new { Message = "Appointment marked as completed successfully." });
		}

		[HttpPost("move-pending/{id:guid}")]
		public async Task<IActionResult> MovePendingAppointment(Guid id, [FromBody] MovePendingAppointmentDto dto)
		{
			await _appointmentService.MovePendingAppointmentAsync(id, dto.Comment);
			return Ok(new { Message = "Appointment status updated to pending." });
		}

		[HttpGet("patients/{patientId:guid}")]
		public async Task<IActionResult> GetPatientDetails([FromHeader(Name = "X-User-Id")] Guid userId, Guid patientId)
		{
			if (userId == Guid.Empty)
			{
				return BadRequest("Missing required X-User-Id header.");
			}

			var result = await _appointmentService.GetPatientDetailsAsync(userId, patientId);
			return Ok(result);
		}
	}
}
