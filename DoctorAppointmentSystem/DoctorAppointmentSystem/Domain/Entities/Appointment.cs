using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoctorAppointmentSystem.Domain.Entities
{
	[Table("Appointments")]
	public class Appointment
	{
		[Key]
		public Guid AppointmentId { get; set; }

		[Required]
		public Patient Patient { get; set; }

		[Required]
		public Doctor Doctor { get; set; }

		public Clinic? Clinic { get; set; }

		[Required]
		public DateTime AppointmentDate { get; set; }

		[Required]
		public DateTime StartTime { get; set; }

		[Required]
		public DateTime EndTime { get; set; }

		[Required]
		public EAppointmentStatus EAppointmentStatus { get; set; }

		[Required]
		[MaxLength(500)]
		public string Reason { get; set; }

		[Required]
		public DateTime CreatedDate { get; set; }

		[Required]
		public EConsultationType EConsultationType { get; set; }
	}

	public enum EConsultationType
	{

	}

	public enum EAppointmentStatus
	{
		Pending,
		Confirmed,
		Cancelled,
		Completed
	}
}
