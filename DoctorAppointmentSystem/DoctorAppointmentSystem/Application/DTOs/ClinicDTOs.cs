using System.ComponentModel.DataAnnotations;

namespace DoctorAppointmentSystem.Application.DTOs
{
	public class RegisterClinicDto
	{
		[Required]
		[MaxLength(150)]
		public string ClinicName { get; set; }

		[Required]
		[MaxLength(50)]
		public string ClinicType { get; set; } // e.g. Clinic, Center, Hospital

		// Address Details
		[Required]
		[MaxLength(100)]
		public string Country { get; set; }

		[Required]
		[MaxLength(100)]
		public string State { get; set; }

		[Required]
		[MaxLength(100)]
		public string City { get; set; }

		[Required]
		[MaxLength(150)]
		public string Area { get; set; }

		[Required]
		[MaxLength(20)]
		public string Pincode { get; set; }

		[Required]
		[MaxLength(250)]
		public string Addressline1 { get; set; }

		[MaxLength(250)]
		public string? Addressline2 { get; set; }

		// Clinic Admin details
		[Required]
		[EmailAddress]
		[MaxLength(150)]
		public string AdminEmail { get; set; }

		[Required]
		[MinLength(6)]
		[MaxLength(100)]
		public string AdminPassword { get; set; }

		[Required]
		[MaxLength(100)]
		public string AdminFirstName { get; set; }

		[Required]
		[MaxLength(100)]
		public string AdminLastName { get; set; }

		[Required]
		[Phone]
		[MaxLength(20)]
		public string AdminMobileNo { get; set; }
	}

	public class CreateClinicDto
	{
		[Required]
		[MaxLength(150)]
		public string ClinicName { get; set; }

		[Required]
		[MaxLength(50)]
		public string ClinicType { get; set; } // e.g. Clinic, Center, Hospital

		[Required]
		[MaxLength(100)]
		public string Country { get; set; }

		[Required]
		[MaxLength(100)]
		public string State { get; set; }

		[Required]
		[MaxLength(100)]
		public string City { get; set; }

		[Required]
		[MaxLength(150)]
		public string Area { get; set; }

		[Required]
		[MaxLength(20)]
		public string Pincode { get; set; }

		[Required]
		[MaxLength(250)]
		public string Addressline1 { get; set; }

		[MaxLength(250)]
		public string? Addressline2 { get; set; }
	}

	public class RegisterAdminForClinicDto
	{
		[Required]
		public Guid ClinicId { get; set; }

		[Required]
		[EmailAddress]
		[MaxLength(150)]
		public string AdminEmail { get; set; }

		[Required]
		[MinLength(6)]
		[MaxLength(100)]
		public string AdminPassword { get; set; }

		[Required]
		[MaxLength(100)]
		public string AdminFirstName { get; set; }

		[Required]
		[MaxLength(100)]
		public string AdminLastName { get; set; }

		[Required]
		[Phone]
		[MaxLength(20)]
		public string AdminMobileNo { get; set; }
	}

	public class ClinicDto
	{
		public Guid ClinicId { get; set; }
		public string ClinicName { get; set; }
		public string ClinicType { get; set; }
		public Guid DoctorId { get; set; }
		public string DoctorName { get; set; }
		public string State { get; set; }
		public string City { get; set; }
		public string Pincode { get; set; } = string.Empty;
		public string Area { get; set; } = string.Empty;
		public string Addressline1 { get; set; } = string.Empty;
		public string? Addressline2 { get; set; }
		public bool IsVerified { get; set; }
		public string VerificationStatus { get; set; } = "Pending";
		public string? RejectionReason { get; set; }
		public bool HasAdmin { get; set; }
	}

	public class UpdateClinicDto
	{
		[Required]
		[MaxLength(150)]
		public string ClinicName { get; set; }

		[Required]
		[MaxLength(50)]
		public string ClinicType { get; set; }

		[Required]
		[MaxLength(100)]
		public string State { get; set; }

		[Required]
		[MaxLength(100)]
		public string City { get; set; }

		[Required]
		[MaxLength(20)]
		public string Pincode { get; set; }

		[Required]
		[MaxLength(100)]
		public string Area { get; set; }

		[Required]
		[MaxLength(250)]
		public string Addressline1 { get; set; }

		[MaxLength(250)]
		public string? Addressline2 { get; set; }
	}

	public class RejectClinicDto
	{
		[Required]
		[MaxLength(500)]
		public string RejectionReason { get; set; }
	}

	public class ClinicAdminDto
	{
		public Guid AdminId { get; set; }
		public Guid UserId { get; set; }
		public Guid ClinicId { get; set; }
		public string ClinicName { get; set; }
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public string MobileNo { get; set; }
		public bool IsVerified { get; set; }
	}
}
