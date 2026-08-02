using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DoctorAppointmentSystem.Application.DTOs;
using DoctorAppointmentSystem.Domain.Entities;

namespace DoctorAppointmentSystem.Application.Services
{
	/// <summary>
	/// Service interface defining appointment booking, scheduling, approval queues, reschedules, audit logs, and status transitions.
	/// </summary>
	public interface IAppointmentService
	{
		/// <summary>Books a new appointment for a patient at a clinic branch or online consultation.</summary>
		Task<AppointmentDto> BookAppointmentAsync(Guid userId, CreateAppointmentDto dto);

		/// <summary>Cancels an existing appointment requested by the patient.</summary>
		Task CancelAppointmentAsync(Guid userId, Guid appointmentId);

		/// <summary>Automatically expires unconfirmed pending appointments whose scheduled date has passed.</summary>
		Task AutoExpirePastPendingAppointmentsAsync();

		/// <summary>Cancels an appointment requested by a doctor or clinic administrator with a reason.</summary>
		Task DoctorCancelAppointmentAsync(Guid userId, Guid appointmentId, string reason);

		/// <summary>Retrieves paginated appointments for the Doctor and Clinic Admin dashboard filters.</summary>
		Task<PagedResult<AppointmentDto>> GetAdminDoctorDashboardAppointmentsAsync(Guid userId, string? status, DateTime? startDate, DateTime? endDate, string? search, Guid? patientId, int page, int size);

		/// <summary>Retrieves paginated patients list for doctor dashboard filters.</summary>
		Task<PagedResult<PatientDto>> GetDashboardPatientsAsync(Guid userId, string? search, int page, int size);

		/// <summary>Retrieves paginated appointments for the Patient portal dashboard.</summary>
		Task<PagedResult<AppointmentDto>> GetPatientDashboardAppointmentsAsync(Guid userId, string? status, bool isHistory, int page, int size);

		/// <summary>Retrieves recently consulted doctors and consultation history for the patient portal.</summary>
		Task<IEnumerable<ConsultedDoctorDto>> GetConsultedDoctorsAsync(Guid userId);

		/// <summary>Retrieves past consultation history with a specific doctor for a patient.</summary>
		Task<IEnumerable<AppointmentDto>> GetDoctorConsultationHistoryAsync(Guid userId, Guid doctorId);

		/// <summary>Retrieves all available verified doctors for booking.</summary>
		Task<IEnumerable<DoctorDto>> GetAvailableDoctorsAsync();

		/// <summary>Retrieves all medical specializations registered in the system.</summary>
		Task<IEnumerable<Specialization>> GetSpecializationsAsync();

		/// <summary>Searches doctors by geographic location, city, state, specialization, or doctor name.</summary>
		Task<IEnumerable<DoctorDto>> SearchDoctorsAsync(string? state, string? city, Guid? specializationId, string? nameSearch);

		/// <summary>Retrieves clinic branches associated with a specific doctor.</summary>
		Task<IEnumerable<ClinicDto>> GetClinicsByDoctorIdAsync(Guid doctorId);

		/// <summary>Retrieves booking details, capacity, and operating hours for a doctor at a clinic branch.</summary>
		Task<BookingDetailsDto> GetBookingDetailsAsync(Guid doctorId, Guid clinicId);

		/// <summary>Approves a pending appointment request and assigns a consultation time slot.</summary>
		Task ApproveAppointmentAsync(Guid userId, Guid appointmentId, string? comment, DateTime? assignedTime = null);

		/// <summary>Rejects a pending appointment request with a rejection reason.</summary>
		Task RejectAppointmentAsync(Guid userId, Guid appointmentId, string reason);

		/// <summary>Completes an appointment, recording clinical comments, prescriptions/reports, and optional follow-up bookings.</summary>
		Task CompleteAppointmentAsync(Guid userId, Guid appointmentId, string? comment, string? report, CreateFollowUpDto? followUp);

		/// <summary>Accepts a proposed follow-up appointment date.</summary>
		Task AcceptFollowUpAsync(Guid userId, Guid appointmentId);

		/// <summary>Declines a proposed follow-up appointment date.</summary>
		Task DeclineFollowUpAsync(Guid userId, Guid appointmentId);

		/// <summary>Moves a scheduled appointment back to the pending queue.</summary>
		Task MovePendingAppointmentAsync(Guid userId, Guid appointmentId, string? comment);

		/// <summary>Retrieves patient demographic details for a specific patient profile ID.</summary>
		Task<PatientDto> GetPatientDetailsAsync(Guid userId, Guid patientId);

		/// <summary>Returns booking capacity info and remaining slots for a specific clinic on a given date.</summary>
		Task<DayAvailabilityDto> GetDayAvailabilityAsync(Guid clinicId, DateTime date);

		/// <summary>Doctor or admin assigns an approximate appointment time to a booked appointment.</summary>
		Task AssignAppointmentTimeAsync(Guid userId, Guid appointmentId, AssignAppointmentTimeDto dto);

		/// <summary>Doctor or admin proposes a new date and time for an appointment.</summary>
		Task ProposeRescheduleAsync(Guid userId, ProposeRescheduleDto dto);

		/// <summary>Patient accepts or declines the proposed reschedule date/time.</summary>
		Task RespondToRescheduleAsync(Guid userId, RespondRescheduleDto dto);

		/// <summary>Gets audit logs for appointments matching filter criteria.</summary>
		Task<PagedResult<AppointmentAuditLogDto>> GetAppointmentAuditLogsAsync(Guid userId, Guid? clinicId, Guid? appointmentId, int page, int size);

		/// <summary>Sets doctor's auto reschedule date preference for leave management.</summary>
		Task SetDoctorAutoRescheduleDateAsync(Guid userId, DateTime? rescheduleDate);

		/// <summary>Sends an HTML email notification for appointment status changes.</summary>
		Task SendAppointmentEmailAsync(
			string toEmail,
			string subject,
			string title,
			string message,
			string doctorName,
			string dateStr,
			string timeOrStatus,
			Clinic? clinic,
			string? patientName = null,
			string? comment = null,
			string? report = null,
			string? followUpStr = null,
			string? cancelledBy = null,
			string? cancelReason = null,
			string? overrideClinicName = null,
			string? overrideClinicAddress = null);
	}
}
