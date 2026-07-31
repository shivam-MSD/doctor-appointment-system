using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoctorAppointmentSystem.Domain.Entities
{
	/// <summary>
	/// Represents a patient demographic record within the HealSync medical system.
	/// Stores identity details, emergency contacts, blood group, and registration metadata.
	/// </summary>
	[Table("Patients")]
	public class Patient
	{
		/// <summary>
		/// Gets or sets the unique primary key identifier for the patient.
		/// </summary>
		[Key]
		public Guid PatientId { get; set; }

		/// <summary>
		/// Gets or sets the first name of the patient.
		/// </summary>
		[Required]
		[MaxLength(100)]
		public string FirstName { get; set; }

		/// <summary>
		/// Gets or sets the last name of the patient.
		/// </summary>
		[Required]
		[MaxLength(100)]
		public string LastName { get; set; }

		/// <summary>
		/// Gets or sets the primary mobile contact number of the patient.
		/// </summary>
		[Required]
		[Phone]
		[MaxLength(20)]
		public string MobileNo { get; set; }

		/// <summary>
		/// Gets or sets the gender enumeration of the patient.
		/// </summary>
		[Required]
		public EGender Gender { get; set; }

		/// <summary>
		/// Gets or sets the date of birth of the patient.
		/// </summary>
		[Required]
		public DateTime DOB { get; set; }

		/// <summary>
		/// Gets or sets the blood group enumeration of the patient.
		/// </summary>
		public EBloodGroup BloodGroup { get; set; }

		/// <summary>
		/// Gets or sets the full name of the emergency contact person.
		/// </summary>
		[MaxLength(100)]
		public string? EmergencyConactName { get; set; }

		/// <summary>
		/// Gets or sets the phone number of the emergency contact person.
		/// </summary>
		[MaxLength(20)]
		public string? EmergencyConactNumber { get; set; }

		/// <summary>
		/// Gets or sets the UTC creation timestamp when the patient record was created.
		/// </summary>
		[Required]
		public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

		/// <summary>
		/// Gets or sets the UTC timestamp when the patient profile was last updated.
		/// </summary>
		public DateTime UpdatedDate { get; set; }
	}

	/// <summary>
	/// Enumeration representing blood group categories.
	/// </summary>
	public enum EBloodGroup
	{
		APositive,
		ANegative,
		BPositive,
		BNegative,
		ABPositive,
		ABNegative,
		OPositive,
		ONegative,
		Unknown
	}
}
