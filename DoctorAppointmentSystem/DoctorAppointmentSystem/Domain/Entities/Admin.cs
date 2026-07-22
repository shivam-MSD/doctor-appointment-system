using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace DoctorAppointmentSystem.Domain.Entities
{
	[Table("Admins")]
	public class Admin
	{
		[Key]
		public Guid AdminId { get; set; }

		[Required]
		public User User { get; set; }

		[Required]
		[MaxLength(100)]
		public string FirstName { get; set; }

		[Required]
		[MaxLength(100)]
		public string LastName { get; set; }

		[Required]
		[MaxLength(20)]
		public string MobileNo { get; set; }

		[Required]
		public bool IsVerified { get; set; } = false;

		/// <summary>One admin can manage multiple clinics via the AdminClinics join table.</summary>
		public ICollection<AdminClinic> AdminClinics { get; set; } = new List<AdminClinic>();

		/// <summary>Read-only convenience: all clinics this admin manages.</summary>
		[NotMapped]
		public IEnumerable<Clinic> Clinics => AdminClinics?.Select(ac => ac.Clinic) ?? Enumerable.Empty<Clinic>();

		/// <summary>
		/// Backward-compatible convenience: returns the first clinic (or null).
		/// Used by existing code that assumes a single-clinic admin.
		/// </summary>
		[NotMapped]
		public Clinic Clinic => AdminClinics?.FirstOrDefault()?.Clinic;

		[Required]
		public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
	}
}
