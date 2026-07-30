using System;
using System.ComponentModel.DataAnnotations;

namespace DoctorAppointmentSystem.Domain.Entities
{
	public class UserPushSubscription
	{
		[Key]
		public Guid Id { get; set; } = Guid.NewGuid();

		[Required]
		public string UserId { get; set; } = string.Empty;

		[Required]
		public string Endpoint { get; set; } = string.Empty;

		public string P256dh { get; set; } = string.Empty;

		public string Auth { get; set; } = string.Empty;

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	}
}
