using DoctorAppointmentSystem.Application.DTOs;
using DoctorAppointmentSystem.Domain.Entities;

namespace DoctorAppointmentSystem.Application.Services
{
	public interface IAppointmentService
	{
		Task<AppointmentDto> BookAppointmentAsync(Guid userId, CreateAppointmentDto dto);
		Task CancelAppointmentAsync(Guid userId, Guid appointmentId);
		Task<PagedResult<AppointmentDto>> GetAdminDoctorDashboardAppointmentsAsync(Guid userId, string? status, DateTime? startDate, DateTime? endDate, string? search, int page, int size);
		Task<PagedResult<PatientDto>> GetDashboardPatientsAsync(Guid userId, string? search, int page, int size);
		Task<PagedResult<AppointmentDto>> GetPatientDashboardAppointmentsAsync(Guid userId, string? status, int page, int size);
		Task<IEnumerable<ConsultedDoctorDto>> GetConsultedDoctorsAsync(Guid userId);
		Task<IEnumerable<DoctorDto>> GetAvailableDoctorsAsync();
		Task<IEnumerable<Specialization>> GetSpecializationsAsync();
		Task<IEnumerable<DoctorDto>> SearchDoctorsAsync(string? state, string? city, Guid? specializationId, string? nameSearch);
		Task<IEnumerable<ClinicDto>> GetClinicsByDoctorIdAsync(Guid doctorId);
		Task<IEnumerable<BookedSlotDto>> GetBookedSlotsAsync(Guid doctorId, Guid clinicId, DateTime date, Guid? patientId);
	}
}
