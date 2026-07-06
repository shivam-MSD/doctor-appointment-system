using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoctorAppointmentSystem.Domain.Entities
{
	[Table("Patients")]
	public class Patient
	{
		[Key]
		public Guid PatientId { get; set; }

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
		public EGender Gender { get; set; }

		[Required]
		public DateTime DOB { get; set; }

		public EBloodGroup BloodGroup { get; set; }

		[MaxLength(100)]
		public string? EmergencyConactName { get; set; }

		[MaxLength(20)]
		public string? EmergencyConactNumber { get; set; }

		[Required]
		public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

		public DateTime UpdatedDate { get; set; }
	}

	public enum EBloodGroup
	{
		APositive,
		ANegative,
		BPositive,
		BNegative,
		ABPositive,
		ABNegative,
		OPositive,
		ONegative
	}

	public enum ERelationshipType
	{
		Self,
		Spouse,
		Child,
		Parent,
		Grandparent,
		Sibling,
		Other
	}
}
