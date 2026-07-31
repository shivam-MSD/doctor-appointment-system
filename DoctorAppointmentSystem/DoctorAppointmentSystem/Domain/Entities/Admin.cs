using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace DoctorAppointmentSystem.Domain.Entities
{
	/// <summary>
	/// Represents a Clinic Administrator entity within the HealSync medical system.
	/// Manages appointments, clinic schedules, and staff profiles for one or multiple assigned clinic branches.
	/// </summary>
	[Table("Admins")]
	public class Admin
	{
		/// <summary>
		/// Gets or sets the unique primary key identifier for the administrator.
		/// </summary>
		[Key]
		public Guid AdminId { get; set; }

		/// <summary>
		/// Gets or sets the associated user authentication identity record.
		/// </summary>
		[Required]
		public User User { get; set; }

		/// <summary>
		/// Gets or sets the first name of the administrator.
		/// </summary>
		[Required]
		[MaxLength(100)]
		public string FirstName { get; set; }

		/// <summary>
		/// Gets or sets the last name of the administrator.
		/// </summary>
		[Required]
		[MaxLength(100)]
		public string LastName { get; set; }

		/// <summary>
		/// Gets or sets the primary mobile contact number of the administrator.
		/// </summary>
		[Required]
		[MaxLength(20)]
		public string MobileNo { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the administrator account has been approved and verified by Super Admin.
		/// </summary>
		[Required]
		public bool IsVerified { get; set; } = false;

		/// <summary>
		/// Gets or sets the gender enumeration of the administrator.
		/// </summary>
		[Required]
		public EGender Gender { get; set; } = EGender.Male;

		/// <summary>
		/// Gets or sets the date of birth of the administrator.
		/// </summary>
		[Required]
		public DateTime DOB { get; set; } = DateTime.MinValue;

		/// <summary>
		/// Gets or sets the collection of junction records mapping this admin to managed clinic branches.
		/// </summary>
		public ICollection<AdminClinic> AdminClinics { get; set; } = new List<AdminClinic>();

		/// <summary>
		/// Gets a read-only collection of all clinic entities managed by this administrator.
		/// </summary>
		[NotMapped]
		public IEnumerable<Clinic> Clinics => AdminClinics?.Select(ac => ac.Clinic) ?? Enumerable.Empty<Clinic>();

		/// <summary>
		/// Gets the primary or first clinic managed by this administrator (convenience navigation property).
		/// </summary>
		[NotMapped]
		public Clinic Clinic => AdminClinics?.FirstOrDefault()?.Clinic;

		/// <summary>
		/// Gets or sets the UTC creation timestamp when the administrator record was registered.
		/// </summary>
		[Required]
		public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
	}
}
