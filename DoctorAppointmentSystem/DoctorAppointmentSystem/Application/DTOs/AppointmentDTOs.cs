using System.ComponentModel.DataAnnotations;

namespace DoctorAppointmentSystem.Application.DTOs
{
	public class AppointmentDto
	{
		public Guid AppointmentId { get; set; }
		public Guid PatientId { get; set; }
		public string PatientName { get; set; }
		public Guid DoctorId { get; set; }
		public string DoctorName { get; set; }
		public Guid? ClinicId { get; set; }
		public string? ClinicName { get; set; }
		public DateTime AppointmentDate { get; set; }
		public DateTime? StartTime { get; set; }
		public DateTime? EndTime { get; set; }
		public string Status { get; set; }
		public string Reason { get; set; }
		public string ConsultationType { get; set; }
		public DateTime CreatedDate { get; set; }
		public string? Comment { get; set; }
		public string? Report { get; set; }
		public string? RejectionReason { get; set; }
		/// <summary>Queue position for this clinic on the appointment date.</summary>
		public int QueueNumber { get; set; }
		/// <summary>Approximate time assigned by the doctor/admin after booking.</summary>
		public DateTime? DoctorAssignedTime { get; set; }
		public DateTime? RescheduleProposedDate { get; set; }
		public DateTime? RescheduleProposedTime { get; set; }
		public string? RescheduleReason { get; set; }
	}

	public class CreateAppointmentDto
	{
		[Required]
		public Guid PatientId { get; set; }

		[Required]
		public Guid DoctorId { get; set; }

		public Guid? ClinicId { get; set; }

		[Required]
		public DateTime AppointmentDate { get; set; }

		[MaxLength(4000, ErrorMessage = "Reason cannot exceed 4000 characters.")]
		public string? Reason { get; set; } = string.Empty;

		[Required]
		public string ConsultationType { get; set; } // e.g., "InPerson", "VideoConsultation"
	}

	public class UpdateAppointmentStatusDto
	{
		[Required]
		public string Status { get; set; } // e.g., "Confirmed", "Cancelled", "Completed"
	}

	public class ConsultedDoctorDto
	{
		public Guid DoctorId { get; set; }
		public string DoctorName { get; set; }
		public string Specialization { get; set; }
		public IEnumerable<AppointmentDto> Appointments { get; set; } = Enumerable.Empty<AppointmentDto>();
	}

	public class ApproveAppointmentDto
	{
		public string? Comment { get; set; }
		public DateTime? DoctorAssignedTime { get; set; }
	}

	public class RejectAppointmentDto
	{
		[Required]
		public string Reason { get; set; }
	}

	public class CompleteAppointmentDto
	{
		public string? Comment { get; set; }
		public string? Report { get; set; }
	}

	public class MovePendingAppointmentDto
	{
		public string? Comment { get; set; }
	}

	/// <summary>Returned by GET /appointments/day-availability to show booking capacity for a date.</summary>
	public class DayAvailabilityDto
	{
		public DateTime Date { get; set; }
		public Guid ClinicId { get; set; }
		public int BookedCount { get; set; }
		public int? MaxCapacity { get; set; } // null = unlimited
		public int? RemainingSlots { get; set; } // null = unlimited
		public bool IsFull { get; set; }
	}

	/// <summary>Posted by doctor/admin to assign a time slot to an existing appointment.</summary>
	public class AssignAppointmentTimeDto
	{
		[Required]
		public DateTime DoctorAssignedTime { get; set; }
		public string? Comment { get; set; }
	}

	public class ProposeRescheduleDto
	{
		[Required]
		public Guid AppointmentId { get; set; }
		[Required]
		public DateTime ProposedDate { get; set; }
		public DateTime? ProposedTime { get; set; }
		[Required]
		public string Reason { get; set; }
	}

	public class RespondRescheduleDto
	{
		[Required]
		public Guid AppointmentId { get; set; }
		[Required]
		public bool Accept { get; set; }
	}
}
