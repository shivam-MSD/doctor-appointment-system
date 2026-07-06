using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoctorAppointmentSystem.Domain.Entities
{
	[Table("Admins")]
	public class Admin
	{
		[Key]
		public Guid AdminId { get; set; }

		[Required]
		public User User { get; set; }

		[Required]
		public Clinic Clinic { get; set; }

		[Required]
		[MaxLength(100)]
		public string FirstName { get; set; }

		[Required]
		[MaxLength(100)]
		public string LastName { get; set; }

		[Required]
		[MaxLength(20)]
		public string MobileNo { get; set; }

		[Required]
		public bool IsVerified { get; set; } = false;

		[Required]
		public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
	}
}
