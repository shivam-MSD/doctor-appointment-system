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
		public DateTime StartTime { get; set; }
		public DateTime EndTime { get; set; }
		public string Status { get; set; }
		public string Reason { get; set; }
		public string ConsultationType { get; set; }
		public DateTime CreatedDate { get; set; }
		public string? Comment { get; set; }
		public string? Report { get; set; }
		public string? RejectionReason { get; set; }
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

		[Required]
		public DateTime StartTime { get; set; }

		[Required]
		public DateTime EndTime { get; set; }

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
}
