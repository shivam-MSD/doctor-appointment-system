using DoctorAppointmentSystem.Application.DTOs;

namespace DoctorAppointmentSystem.Application.Services
{
	public interface IPatientService
	{
		Task<PatientDto> GetPatientProfileAsync(Guid userId, Guid patientId);
		Task<PatientDto> UpdatePatientProfileAsync(Guid userId, Guid patientId, PatientUpdateDto dto);
		Task<PagedResult<DoctorDto>> GetDoctorsForPatientAsync(
			string? search,
			Guid? specializationId,
			string? state,
			string? city,
			int page,
			int size);
	}
}
