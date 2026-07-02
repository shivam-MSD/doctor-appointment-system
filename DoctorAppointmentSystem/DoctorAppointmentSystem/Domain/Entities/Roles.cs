using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoctorAppointmentSystem.Domain.Entities
{
	[Table("Roles")]
	public class Roles
	{
		[Key]
		public Guid RoleId { get; set; }

		[Required]
		public ERole Role { get; set; }
	}

	public enum ERole
	{
		SuperAdmin,
		Admin,
		Doctor,
		Patient
	}
}
