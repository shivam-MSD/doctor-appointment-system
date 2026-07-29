using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoctorAppointmentSystem.Domain.Entities
{
	[Table("UserPasswords")]
	public class UserPassword
	{
		[Key]
		[ForeignKey("User")]
		public Guid UserId { get; set; }

		[Required]
		public User User { get; set; }

		[Required]
		[MaxLength(500)]
		public string PasswordHash { get; set; }
	}
}
