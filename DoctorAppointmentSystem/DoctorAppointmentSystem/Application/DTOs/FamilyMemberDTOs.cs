using System.ComponentModel.DataAnnotations;

namespace DoctorAppointmentSystem.Application.DTOs
{
	public class AddFamilyMemberDto
	{
		[Required]
		public string FirstName { get; set; }

		[Required]
		public string LastName { get; set; }

		[Required]
		[Phone]
		public string MobileNo { get; set; }

		[Required]
		public string Gender { get; set; } // "Male", "Female", "Other"

		[Required]
		public DateTime DOB { get; set; }

		[Required]
		public string RelationshipType { get; set; } // e.g., "Spouse", "Child", "Parent", "Grandparent", "Sibling", "Other"
	}

	public class VerifyFamilyOtpDto
	{
		[Required]
		public Guid VerificationId { get; set; }

		[Required]
		[StringLength(6, MinimumLength = 4, ErrorMessage = "OTP must be between 4 and 6 digits.")]
		public string OtpCode { get; set; }
	}
}
