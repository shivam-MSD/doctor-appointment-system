using System.ComponentModel.DataAnnotations;

namespace DoctorAppointmentSystem.Application.DTOs
{
	public class ChangePasswordDto
	{
		[Required]
		public string CurrentPassword { get; set; }

		[Required]
		[MinLength(6, ErrorMessage = "New password must be at least 6 characters long.")]
		public string NewPassword { get; set; }
	}
}
