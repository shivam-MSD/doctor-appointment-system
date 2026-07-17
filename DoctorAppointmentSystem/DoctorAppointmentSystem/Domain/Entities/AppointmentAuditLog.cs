using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoctorAppointmentSystem.Domain.Entities
{
	[Table("AppointmentAuditLogs")]
	public class AppointmentAuditLog
	{
		[Key]
		public Guid LogId { get; set; }

		[Required]
		public Guid AppointmentId { get; set; }

		[Required]
		[MaxLength(100)]
		public string Action { get; set; } // e.g. "Created", "RescheduleProposed", "Confirmed", "Cancelled", "Completed", "Skipped"

		public Guid? ActorUserId { get; set; }

		[MaxLength(200)]
		public string? ActorName { get; set; }

		[MaxLength(50)]
		public string? ActorRole { get; set; } // e.g. "Patient", "Doctor", "Admin", "System"

		[Required]
		public DateTime Timestamp { get; set; }

		[MaxLength(1000)]
		public string? Notes { get; set; }

		// Navigation properties
		[ForeignKey("AppointmentId")]
		public virtual Appointment Appointment { get; set; }
	}
}
