using System;
using System.ComponentModel.DataAnnotations;

namespace DoctorAppointmentSystem.Application.DTOs
{
	/// <summary>
	/// DTO payload for adding a dependent family member (Children, Toddlers, Elderly Parents) without phone/email.
	/// </summary>
	public class CreateDependentDto
	{
		[Required]
		public string FirstName { get; set; }

		[Required]
		public string LastName { get; set; }

		[Required]
		public string Gender { get; set; } // "Male", "Female", "Other"

		[Required]
		public DateTime DOB { get; set; }

		[Required]
		public string RelationshipType { get; set; } // "Spouse", "Child", "Parent", "Grandparent", "Sibling", "Other"

		public string? BloodGroup { get; set; }

		[Required]
		public bool ConsentDeclared { get; set; } = true;
	}

	/// <summary>
	/// DTO payload to send 6-digit OTP code to an adult family member via Email and/or WhatsApp.
	/// </summary>
	public class SendFamilyLinkOtpDto
	{
		[Required]
		public string TargetContact { get; set; } // Email ID or Mobile Number

		/// <summary>
		/// Delivery channel: "Email", "WhatsApp", or "Both"
		/// </summary>
		[Required]
		public string Channel { get; set; } = "Both";

		[Required]
		public string RelationshipType { get; set; }
	}

	/// <summary>
	/// DTO payload to verify 6-digit OTP code and link adult family member account.
	/// </summary>
	public class VerifyFamilyLinkOtpDto
	{
		[Required]
		public string TargetContact { get; set; } // Email ID or Mobile Number

		[Required]
		[StringLength(10, MinimumLength = 4, ErrorMessage = "OTP must be between 4 and 10 digits.")]
		public string OtpCode { get; set; }

		[Required]
		public string RelationshipType { get; set; }
	}

	/// <summary>
	/// Data transfer object representing a family member returned to Angular client.
	/// </summary>
	public class FamilyMemberDetailDto
	{
		public Guid PatientId { get; set; }
		public string FullName { get; set; }
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public string RelationshipType { get; set; }
		public string Gender { get; set; }
		public DateTime DOB { get; set; }
		public int Age { get; set; }
		public string? BloodGroup { get; set; }
		public bool IsDependent { get; set; }
		public bool IsVerified { get; set; }
		public string? Email { get; set; }
		public string? MobileNo { get; set; }
		public DateTime CreatedDate { get; set; }
	}

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
		public string Gender { get; set; }

		[Required]
		public DateTime DOB { get; set; }

		[Required]
		public string RelationshipType { get; set; }
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
