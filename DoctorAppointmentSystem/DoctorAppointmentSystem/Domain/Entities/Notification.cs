using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoctorAppointmentSystem.Domain.Entities
{
	[Table("Notifications")]
	public class Notification
	{
		[Key]
		public Guid NotificationId { get; set; }

		[Required]
		public Guid UserId { get; set; }

		[ForeignKey("UserId")]
		public virtual User User { get; set; }

		[Required]
		[MaxLength(500)]
		public string Message { get; set; }

		[Required]
		public bool IsRead { get; set; } = false;

		[Required]
		public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
	}
}
