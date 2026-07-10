namespace DoctorAppointmentSystem.Application.DTOs
{
	public class UserDto
	{
		public Guid UserId { get; set; }
		public string Email { get; set; }
		public bool IsActive { get; set; }
	}

	public class DoctorProfileDto
	{
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public string MobileNo { get; set; }
		public string Qualification { get; set; }
		public string LicenceNumber { get; set; }
		public int YearsOfExperience { get; set; }
		public double ConsultationFee { get; set; }
		public string AboutDoctor { get; set; }
		public Guid? SpecializationId { get; set; }

		// Address fields
		public string Country { get; set; } = string.Empty;
		public string State { get; set; } = string.Empty;
		public string City { get; set; } = string.Empty;
		public string Area { get; set; } = string.Empty;
		public string Pincode { get; set; } = string.Empty;
		public string Addressline1 { get; set; } = string.Empty;
		public string? Addressline2 { get; set; }
	}

	public class AdminProfileDto
	{
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public string MobileNo { get; set; }
		public string ClinicName { get; set; }

		// Address fields
		public string Country { get; set; } = string.Empty;
		public string State { get; set; } = string.Empty;
		public string City { get; set; } = string.Empty;
		public string Area { get; set; } = string.Empty;
		public string Pincode { get; set; } = string.Empty;
		public string Addressline1 { get; set; } = string.Empty;
		public string? Addressline2 { get; set; }
	}
}
