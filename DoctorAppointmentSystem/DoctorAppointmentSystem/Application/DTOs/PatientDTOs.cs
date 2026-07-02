using System.ComponentModel.DataAnnotations;

namespace DoctorAppointmentSystem.Application.DTOs
{
	public class PatientDto
	{
		public Guid PatientId { get; set; }
		public Guid UserId { get; set; }
		public string Email { get; set; }
		public string BloodGroup { get; set; }
		public string EmergencyContactName { get; set; }
		public string EmergencyContactNumber { get; set; }
	}

	public class PatientRegisterDto
	{
		[Required]
		public Guid UserId { get; set; }

		[Required]
		public string BloodGroup { get; set; } // e.g., "APositive", "ONegative"

		[Required]
		public string EmergencyContactName { get; set; }

		[Required]
		[Phone]
		public string EmergencyContactNumber { get; set; }
	}
}
