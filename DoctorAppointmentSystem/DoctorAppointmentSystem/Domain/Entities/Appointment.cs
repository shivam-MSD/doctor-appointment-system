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

		[MaxLength(4000)]
		public string? Reason { get; set; }

		[Required]
		public DateTime CreatedDate { get; set; }

		[Required]
		public EConsultationType EConsultationType { get; set; }

		[MaxLength(1000)]
		public string? Comment { get; set; }

		[MaxLength(2000)]
		public string? Report { get; set; }

		[MaxLength(500)]
		public string? RejectionReason { get; set; }
	}

	public enum EConsultationType
	{
		InPerson,
		VideoConsultation
	}

	public enum EAppointmentStatus
	{
		Pending,
		Confirmed,
		Cancelled,
		Completed,
		Rejected
	}
}
