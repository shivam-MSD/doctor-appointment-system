using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoctorAppointmentSystem.Domain.Entities
{
	/// <summary>
	/// Represents an audit log entry recording verification approvals, branch updates, and status changes for clinic locations.
	/// </summary>
	[Table("ClinicAuditLogs")]
	public class ClinicAuditLog
	{
		/// <summary>
		/// Gets or sets the unique primary key identifier for the audit log record.
		/// </summary>
		[Key]
		public Guid LogId { get; set; }

		/// <summary>
		/// Gets or sets the associated clinic branch ID.
		/// </summary>
		[Required]
		public Guid ClinicId { get; set; }

		/// <summary>
		/// Gets or sets the action classification name (e.g. "Approved", "Rejected", "Updated", "Deactivated").
		/// </summary>
		[Required]
		[MaxLength(100)]
		public string Action { get; set; }

		/// <summary>
		/// Gets or sets the user ID of the actor who performed the action.
		/// </summary>
		public Guid? ActorUserId { get; set; }

		/// <summary>
		/// Gets or sets the human-readable display name of the actor who performed the action.
		/// </summary>
		[MaxLength(200)]
		public string? ActorName { get; set; }

		/// <summary>
		/// Gets or sets the UTC timestamp when the audit event occurred.
		/// </summary>
		[Required]
		public DateTime Timestamp { get; set; }

		/// <summary>
		/// Gets or sets the JSON representation of the clinic state before modification.
		/// </summary>
		[Required]
		public string OldDataJson { get; set; }

		/// <summary>
		/// Gets or sets the JSON representation of the clinic state after modification.
		/// </summary>
		[Required]
		public string NewDataJson { get; set; }

		/// <summary>
		/// Gets or sets additional explanatory notes or contextual details regarding the audit event.
		/// </summary>
		public string? Notes { get; set; }
	}
}
