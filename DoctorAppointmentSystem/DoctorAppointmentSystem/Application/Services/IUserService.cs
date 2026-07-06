using DoctorAppointmentSystem.Application.DTOs;

namespace DoctorAppointmentSystem.Application.Services
{
	public interface IUserService
	{
		Task<UserDto> GetUserProfileAsync(Guid userId);
		Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto);
		Task<DoctorProfileDto> GetDoctorProfileAsync(Guid userId);
		Task<DoctorProfileDto> UpdateDoctorProfileAsync(Guid userId, DoctorProfileDto dto);
		Task<AdminProfileDto> GetAdminProfileAsync(Guid userId);
		Task<AdminProfileDto> UpdateAdminProfileAsync(Guid userId, AdminProfileDto dto);
	}
}
