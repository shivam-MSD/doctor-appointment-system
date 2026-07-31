using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoctorAppointmentSystem.Domain.Entities
{
	/// <summary>
	/// Represents an authorization role entity within the HealSync medical system database.
	/// Maps system roles (SuperAdmin, Admin, Doctor, Patient) to role identifiers.
	/// </summary>
	[Table("Roles")]
	public class Roles
	{
		/// <summary>
		/// Gets or sets the unique primary key identifier for the role.
		/// </summary>
		[Key]
		public Guid RoleId { get; set; }

		/// <summary>
		/// Gets or sets the role enumeration value (SuperAdmin, Admin, Doctor, Patient).
		/// </summary>
		[Required]
		public ERole Role { get; set; }
	}

	/// <summary>
	/// Enumeration representing the primary access control roles in the system.
	/// </summary>
	public enum ERole
	{
		/// <summary>System Super Administrator with overall portal management privileges.</summary>
		SuperAdmin,
		/// <summary>Clinic Administrator managing specific clinic branch operations.</summary>
		Admin,
		/// <summary>Medical Doctor / Practitioner managing patient consultations.</summary>
		Doctor,
		/// <summary>Patient seeking medical appointments and health services.</summary>
		Patient
	}
}
