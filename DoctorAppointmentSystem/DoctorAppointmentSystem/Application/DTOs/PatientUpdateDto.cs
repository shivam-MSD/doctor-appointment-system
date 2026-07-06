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

		[MaxLength(100)]
		public string? EmergencyContactName { get; set; }

		[Phone]
		[MaxLength(20)]
		public string? EmergencyContactNumber { get; set; }

		// Address fields
		[Required]
		public string Country { get; set; } = "India";
		[Required]
		public string State { get; set; } = string.Empty;
		[Required]
		public string City { get; set; } = string.Empty;
		[Required]
		public string Area { get; set; } = string.Empty;
		[Required]
		public string Pincode { get; set; } = string.Empty;
		[Required]
		public string Addressline1 { get; set; } = string.Empty;
		public string? Addressline2 { get; set; }
	}
}
