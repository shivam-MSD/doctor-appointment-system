using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoctorAppointmentSystem.Domain.Entities
{
	[Table("AdminAuditLogs")]
	public class AdminAuditLog
	{
		[Key]
		public Guid LogId { get; set; }

		[Required]
		public Guid AdminId { get; set; }

		[Required]
		[MaxLength(100)]
		public string Action { get; set; } // e.g. "Registered", "Verified", "Rejected", "Updated"

		public Guid? ActorUserId { get; set; }

		[MaxLength(200)]
		public string? ActorName { get; set; }

		[Required]
		public DateTime Timestamp { get; set; }

		[Required]
		public string OldDataJson { get; set; }

		[Required]
		public string NewDataJson { get; set; }

		[MaxLength(500)]
		public string? Notes { get; set; }
	}
}
