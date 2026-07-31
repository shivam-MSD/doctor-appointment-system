using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoctorAppointmentSystem.Domain.Entities
{
	/// <summary>
	/// Represents an in-app system notification alert sent to a user.
	/// Stores notification text message, recipient user ID, read status, and creation timestamp.
	/// </summary>
	[Table("Notifications")]
	public class Notification
	{
		/// <summary>
		/// Gets or sets the unique primary key identifier for the notification.
		/// </summary>
		[Key]
		public Guid NotificationId { get; set; }

		/// <summary>
		/// Gets or sets the recipient user account ID.
		/// </summary>
		[Required]
		public Guid UserId { get; set; }

		/// <summary>
		/// Gets or sets the associated User navigation entity.
		/// </summary>
		[ForeignKey("UserId")]
		public virtual User User { get; set; }

		/// <summary>
		/// Gets or sets the notification alert message content.
		/// </summary>
		[Required]
		[MaxLength(500)]
		public string Message { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the user has read or acknowledged the notification.
		/// </summary>
		[Required]
		public bool IsRead { get; set; } = false;

		/// <summary>
		/// Gets or sets the UTC timestamp when the notification was dispatched.
		/// </summary>
		[Required]
		public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
	}
}
