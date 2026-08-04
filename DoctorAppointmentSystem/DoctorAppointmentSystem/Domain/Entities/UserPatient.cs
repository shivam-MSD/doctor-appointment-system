using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoctorAppointmentSystem.Domain.Entities
{
	/// <summary>
	/// Represents a junction relationship mapping between a User account and a Patient profile (e.g. Self, Spouse, Child, Parent).
	/// Supports multi-family patient management under a single primary user account.
	/// </summary>
	[Table("UserPatients")]
	public class UserPatient
	{
		/// <summary>
		/// Gets or sets the associated user account ID.
		/// </summary>
		[Required]
		public Guid UserId { get; set; }

		/// <summary>
		/// Gets or sets the associated User entity.
		/// </summary>
		public User User { get; set; }

		/// <summary>
		/// Gets or sets the associated patient profile ID.
		/// </summary>
		[Required]
		public Guid PatientId { get; set; }

		/// <summary>
		/// Gets or sets the associated Patient entity.
		/// </summary>
		public Patient Patient { get; set; }

		/// <summary>
		/// Gets or sets the family relationship type enumeration between the user and patient (Self, Spouse, Child, Parent, Other).
		/// </summary>
		[Required]
		public ERelationshipType RelationshipType { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the family member relationship has been verified via OTP.
		/// </summary>
		[Required]
		public bool IsVerified { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the family member is a dependent without phone/email.
		/// </summary>
		public bool IsDependent { get; set; } = true;

		/// <summary>
		/// Gets or sets the 6-digit OTP code for family link verification.
		/// </summary>
		[MaxLength(10)]
		public string? VerificationOtp { get; set; }

		/// <summary>
		/// Gets or sets the UTC expiry timestamp for the OTP code.
		/// </summary>
		public DateTime? OtpExpiryTime { get; set; }

		/// <summary>
		/// Gets or sets the OTP delivery channel string ("Email", "WhatsApp", "Both").
		/// </summary>
		[MaxLength(20)]
		public string? OtpChannel { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether legal guardian consent declaration was accepted.
		/// </summary>
		public bool ConsentDeclared { get; set; } = true;

		/// <summary>
		/// Gets or sets the UTC creation timestamp when the relationship mapping was created.
		/// </summary>
		[Required]
		public DateTime CreatedDate { get; set; }
	}

	/// <summary>
	/// Enumeration representing relationship types between a primary user account and linked patient profiles.
	/// </summary>
	public enum ERelationshipType
	{
		Self,
		Spouse,
		Child,
		Parent,
		Other
	}
}
