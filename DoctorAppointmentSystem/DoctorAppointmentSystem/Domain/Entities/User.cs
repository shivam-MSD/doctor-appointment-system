using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoctorAppointmentSystem.Domain.Entities
{
	[Table("Users")]
	public class User
	{
		[Key]
		public Guid UserId { get; set; }

		[Required]
		[EmailAddress]
		[MaxLength(150)]
		public string Email { get; set; }

		[Required]
		[MaxLength(500)]
		public string PasswordHash { get; set; }

		[Required]
		public bool IsActive { get; set; } = true;

		[Required]
		public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

		public DateTime LastLoginDate { get; set; }
	}
}
