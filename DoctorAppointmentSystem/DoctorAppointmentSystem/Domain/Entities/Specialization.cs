using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoctorAppointmentSystem.Domain.Entities
{
	[Table("Specializations")]
	public class Specialization
	{
		[Key]
		public Guid SpecializationId { get; set; }

		[Required]
		[MaxLength(100)]
		public string SpecializationName { get; set; }
	}
}
