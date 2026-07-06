using DoctorAppointmentSystem.Application.DTOs;

namespace DoctorAppointmentSystem.Application.Services
{
	public interface IAuthService
	{
		Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto);
		Task<AuthResponseDto> RegisterDoctorAsync(DoctorSignUpDto doctorSignUpDto);
		Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
		Task<AuthResponseDto> VerifyEmailAsync(VerifyEmailDto dto);
	}
}
