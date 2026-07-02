using DoctorAppointmentSystem.Application.DTOs;

namespace DoctorAppointmentSystem.Application.Services
{
	public interface IAuthService
	{
		Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto);
		Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
	}
}
