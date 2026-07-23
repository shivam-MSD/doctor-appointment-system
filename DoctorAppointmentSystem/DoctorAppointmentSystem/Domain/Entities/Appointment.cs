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

		// Time fields are now optional — no longer required from patient at booking time.
		// Doctor assigns DoctorAssignedTime after reviewing the patient queue.
		public DateTime? StartTime { get; set; }

		public DateTime? EndTime { get; set; }

		/// <summary>Sequential queue position for this clinic on the appointment date. e.g. Patient is #3 for Monday.</summary>
		public int QueueNumber { get; set; } = 0;

		/// <summary>Time assigned by doctor/admin after the appointment is booked. Shown to patient on dashboard.</summary>
		public DateTime? DoctorAssignedTime { get; set; }

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

		public DateTime? RescheduleProposedDate { get; set; }

		public DateTime? RescheduleProposedTime { get; set; }

		[MaxLength(500)]
		public string? RescheduleReason { get; set; }

		// Detailed Status Timestamps
		public DateTime? ConfirmedDate { get; set; }
		public DateTime? RescheduleProposedAt { get; set; }
		public DateTime? CancelledDate { get; set; }
		
		[MaxLength(50)]
		public string? CancelledBy { get; set; } // "Patient", "Doctor", or "Admin"
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
		Rejected,
		Expired,
		RescheduleProposed,
		FollowUpProposed
	}
}
