using DoctorAppointmentSystem.Application.DTOs;

namespace DoctorAppointmentSystem.Application.Services
{
	public interface IFamilyService
	{
		Task<Guid> InitiateAddFamilyMemberAsync(Guid userId, AddFamilyMemberDto dto);
		Task<PatientDto> VerifyFamilyMemberOtpAsync(Guid userId, VerifyFamilyOtpDto dto);
		Task<IEnumerable<PatientDto>> GetFamilyMembersAsync(Guid userId);
	}
}
