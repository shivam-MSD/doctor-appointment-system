using System.ComponentModel.DataAnnotations;

namespace DoctorAppointmentSystem.Application.DTOs
{
	public class PatientDto
	{
		public Guid PatientId { get; set; }
		public Guid UserId { get; set; }
		public string Email { get; set; }
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public string MobileNo { get; set; }
		public string Gender { get; set; }
		public DateTime DOB { get; set; }
		public int Age { get; set; }
		public string BloodGroup { get; set; }
		public string EmergencyContactName { get; set; }
		public string EmergencyContactNumber { get; set; }

		// Address fields
		public string Country { get; set; } = string.Empty;
		public string State { get; set; } = string.Empty;
		public string City { get; set; } = string.Empty;
		public string Area { get; set; } = string.Empty;
		public string Pincode { get; set; } = string.Empty;
		public string Addressline1 { get; set; } = string.Empty;
		public string? Addressline2 { get; set; }
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
