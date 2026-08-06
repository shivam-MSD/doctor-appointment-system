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
		Task<DoctorDto> GetDoctorDetailsForPatientAsync(Guid doctorId);

		// Family Member Management
		Task<IEnumerable<FamilyMemberDetailDto>> GetFamilyMembersAsync(Guid userId);
		Task<FamilyMemberDetailDto> CreateDependentFamilyMemberAsync(Guid userId, CreateDependentDto dto);
		Task<object> SendFamilyLinkOtpAsync(Guid userId, SendFamilyLinkOtpDto dto);
		Task<FamilyMemberDetailDto> VerifyFamilyLinkOtpAsync(Guid userId, VerifyFamilyLinkOtpDto dto);
		Task DeleteFamilyMemberAsync(Guid userId, Guid familyPatientId);

		// Contact Info Updates (Email & WhatsApp)
		Task<object> InitiateUpdateContactInfoAsync(Guid userId, InitiateContactUpdateDto dto);
		Task<PatientDto> ConfirmUpdateContactInfoAsync(Guid userId, ConfirmContactUpdateDto dto);
	}
}
