using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoctorAppointmentSystem.Domain.Entities
{
	/// <summary>
	/// Represents a medical specialty or clinical domain category (e.g., Cardiology, Dermatology, Orthopedics, Pediatrics).
	/// </summary>
	[Table("Specializations")]
	public class Specialization
	{
		/// <summary>
		/// Gets or sets the unique primary key identifier for the specialization.
		/// </summary>
		[Key]
		public Guid SpecializationId { get; set; }

		/// <summary>
		/// Gets or sets the medical specialization name string.
		/// </summary>
		[Required]
		[MaxLength(100)]
		public string SpecializationName { get; set; }
	}
}
