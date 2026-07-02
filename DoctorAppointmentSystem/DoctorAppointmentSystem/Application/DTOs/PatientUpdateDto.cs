using System.ComponentModel.DataAnnotations;

namespace DoctorAppointmentSystem.Application.DTOs
{
	public class PatientUpdateDto
	{
		[Required]
		[MaxLength(100)]
		public string FirstName { get; set; }

		[Required]
		[MaxLength(100)]
		public string LastName { get; set; }

		[Required]
		[Phone]
		[MaxLength(20)]
		public string MobileNo { get; set; }

		[Required]
		[MaxLength(10)]
		public string Gender { get; set; }

		[Required]
		public DateTime DOB { get; set; }

		public string BloodGroup { get; set; } // e.g. "APositive", "ONegative"

		[Required]
		[MaxLength(100)]
		public string EmergencyContactName { get; set; }

		[Required]
		[Phone]
		[MaxLength(20)]
		public string EmergencyContactNumber { get; set; }
	}
}
