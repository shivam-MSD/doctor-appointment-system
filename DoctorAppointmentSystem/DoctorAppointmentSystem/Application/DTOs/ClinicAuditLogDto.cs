using System;

namespace DoctorAppointmentSystem.Application.DTOs
{
	public class ClinicAuditLogDto
	{
		public Guid LogId { get; set; }
		public Guid ClinicId { get; set; }
		public string Action { get; set; }
		public Guid? ActorUserId { get; set; }
		public string? ActorName { get; set; }
		public DateTime Timestamp { get; set; }
		public string OldDataJson { get; set; }
		public string NewDataJson { get; set; }
		public string? Notes { get; set; }
	}
}
