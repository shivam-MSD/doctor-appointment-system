using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoctorAppointmentSystem.Domain.Entities
{
	[Table("UserPatients")]
	public class UserPatient
	{
		[Required]
		public Guid UserId { get; set; }
		public User User { get; set; }

		[Required]
		public Guid PatientId { get; set; }
		public Patient Patient { get; set; }

		[Required]
		public ERelationshipType RelationshipType { get; set; }

		[Required]
		public bool IsVerified { get; set; }

		[Required]
		public DateTime CreatedDate { get; set; }
	}
}
