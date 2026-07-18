using System;

namespace DoctorAppointmentSystem.Application.DTOs
{
	public class SystemAuditLogDto
	{
		public Guid LogId { get; set; }
		public string EntityType { get; set; } = string.Empty; // "Clinic", "Doctor", "Admin"
		public Guid EntityId { get; set; }
		public string EntityName { get; set; } = string.Empty;
		public string Action { get; set; } = string.Empty;
		public Guid? ActorUserId { get; set; }
		public string? ActorName { get; set; }
		public DateTime Timestamp { get; set; }
		public string OldDataJson { get; set; } = string.Empty;
		public string NewDataJson { get; set; } = string.Empty;
		public string? Notes { get; set; }
	}
}
