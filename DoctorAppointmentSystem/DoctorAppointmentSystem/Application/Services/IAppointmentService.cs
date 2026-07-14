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
		Task<PagedResult<AppointmentDto>> GetPatientDashboardAppointmentsAsync(Guid userId, string? status, int page, int size);
		Task<IEnumerable<ConsultedDoctorDto>> GetConsultedDoctorsAsync(Guid userId);
		Task<IEnumerable<DoctorDto>> GetAvailableDoctorsAsync();
		Task<IEnumerable<Specialization>> GetSpecializationsAsync();
		Task<IEnumerable<DoctorDto>> SearchDoctorsAsync(string? state, string? city, Guid? specializationId, string? nameSearch);
		Task<IEnumerable<ClinicDto>> GetClinicsByDoctorIdAsync(Guid doctorId);
		Task<IEnumerable<BookedSlotDto>> GetBookedSlotsAsync(Guid doctorId, Guid clinicId, DateTime date, Guid? patientId);
		Task<BookingDetailsDto> GetBookingDetailsAsync(Guid doctorId, Guid clinicId);
		Task ApproveAppointmentAsync(Guid appointmentId, string? comment);
		Task RejectAppointmentAsync(Guid appointmentId, string reason);
		Task CompleteAppointmentAsync(Guid appointmentId, string? comment, string? report);
		Task MovePendingAppointmentAsync(Guid appointmentId, string? comment);
		Task<PatientDto> GetPatientDetailsAsync(Guid userId, Guid patientId);
	}
}
