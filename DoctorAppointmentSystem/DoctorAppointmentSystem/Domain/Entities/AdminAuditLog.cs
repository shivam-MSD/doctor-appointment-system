using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoctorAppointmentSystem.Domain.Entities
{
	/// <summary>
	/// Represents an audit trail log entry recording administrative actions performed by or on Clinic Administrators and Super Admins.
	/// Stores action types, timestamps, actor identity, and JSON state snapshots.
	/// </summary>
	[Table("AdminAuditLogs")]
	public class AdminAuditLog
	{
		/// <summary>
		/// Gets or sets the unique primary key identifier for the audit log record.
		/// </summary>
		[Key]
		public Guid LogId { get; set; }

		/// <summary>
		/// Gets or sets the targeted administrator ID associated with the audit event.
		/// </summary>
		[Required]
		public Guid AdminId { get; set; }

		/// <summary>
		/// Gets or sets the action classification name (e.g. "LOGIN", "Registered", "Verified", "Rejected", "Updated").
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
		/// Gets or sets the JSON representation of the entity state before the modification.
		/// </summary>
		[Required]
		public string OldDataJson { get; set; }

		/// <summary>
		/// Gets or sets the JSON representation of the entity state after the modification.
		/// </summary>
		[Required]
		public string NewDataJson { get; set; }

		/// <summary>
		/// Gets or sets additional explanatory notes or contextual details regarding the audit event.
		/// </summary>
		public string? Notes { get; set; }
	}
}
