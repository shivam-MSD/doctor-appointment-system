namespace DoctorAppointmentSystem.Application.DTOs
{
	public class UserDto
	{
		public Guid UserId { get; set; }
		public string Email { get; set; }
		public bool IsActive { get; set; }
	}
}
