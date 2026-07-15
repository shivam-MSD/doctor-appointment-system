using DoctorAppointmentSystem.Application.DTOs;
using DoctorAppointmentSystem.Domain.Entities;

namespace DoctorAppointmentSystem.Application.Services
{
	public interface IAppointmentService
	{
		Task<AppointmentDto> BookAppointmentAsync(Guid userId, CreateAppointmentDto dto);
		Task CancelAppointmentAsync(Guid userId, Guid appointmentId);
		Task AutoExpirePastPendingAppointmentsAsync();
		Task DoctorCancelAppointmentAsync(Guid userId, Guid appointmentId, string reason);
		Task<PagedResult<AppointmentDto>> GetAdminDoctorDashboardAppointmentsAsync(Guid userId, string? status, DateTime? startDate, DateTime? endDate, string? search, Guid? patientId, int page, int size);
		Task<PagedResult<PatientDto>> GetDashboardPatientsAsync(Guid userId, string? search, int page, int size);
		Task<PagedResult<AppointmentDto>> GetPatientDashboardAppointmentsAsync(Guid userId, string? status, bool isHistory, int page, int size);
		Task<IEnumerable<ConsultedDoctorDto>> GetConsultedDoctorsAsync(Guid userId);
		Task<IEnumerable<DoctorDto>> GetAvailableDoctorsAsync();
		Task<IEnumerable<Specialization>> GetSpecializationsAsync();
		Task<IEnumerable<DoctorDto>> SearchDoctorsAsync(string? state, string? city, Guid? specializationId, string? nameSearch);
		Task<IEnumerable<ClinicDto>> GetClinicsByDoctorIdAsync(Guid doctorId);
		Task<BookingDetailsDto> GetBookingDetailsAsync(Guid doctorId, Guid clinicId);
		Task ApproveAppointmentAsync(Guid appointmentId, string? comment, DateTime? assignedTime = null);
		Task RejectAppointmentAsync(Guid appointmentId, string reason);
		Task CompleteAppointmentAsync(Guid appointmentId, string? comment, string? report);
		Task MovePendingAppointmentAsync(Guid appointmentId, string? comment);
		Task<PatientDto> GetPatientDetailsAsync(Guid userId, Guid patientId);

		/// <summary>Returns booking capacity info for a specific clinic on a given date.</summary>
		Task<DayAvailabilityDto> GetDayAvailabilityAsync(Guid clinicId, DateTime date);

		/// <summary>Doctor or admin assigns an approximate appointment time to a booked appointment.</summary>
		Task AssignAppointmentTimeAsync(Guid userId, Guid appointmentId, AssignAppointmentTimeDto dto);

		/// <summary>Doctor or admin proposes a new date and time for an appointment.</summary>
		Task ProposeRescheduleAsync(Guid userId, ProposeRescheduleDto dto);

		/// <summary>Patient accepts or declines the proposed reschedule.</summary>
		Task RespondToRescheduleAsync(Guid userId, RespondRescheduleDto dto);
	}
}
