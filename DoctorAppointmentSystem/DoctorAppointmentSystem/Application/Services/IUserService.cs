using DoctorAppointmentSystem.Application.DTOs;

namespace DoctorAppointmentSystem.Application.Services
{
	public interface IUserService
	{
		Task<UserDto> GetUserProfileAsync(Guid userId);
		Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto);
	}
}
