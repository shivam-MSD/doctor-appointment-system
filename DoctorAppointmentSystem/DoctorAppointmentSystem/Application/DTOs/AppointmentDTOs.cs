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
		public DateTime AppointmentDate { get; set; }
		public DateTime StartTime { get; set; }
		public DateTime EndTime { get; set; }
		public string Status { get; set; }
		public string Reason { get; set; }
		public string ConsultationType { get; set; }
		public DateTime CreatedDate { get; set; }
	}

	public class CreateAppointmentDto
	{
		[Required]
		public Guid PatientId { get; set; }

		[Required]
		public Guid DoctorId { get; set; }

		[Required]
		public DateTime AppointmentDate { get; set; }

		[Required]
		public DateTime StartTime { get; set; }

		[Required]
		public DateTime EndTime { get; set; }

		[Required]
		[MaxLength(500, ErrorMessage = "Reason cannot exceed 500 characters.")]
		public string Reason { get; set; }

		[Required]
		public string ConsultationType { get; set; } // e.g., "InPerson", "VideoConsultation"
	}

	public class UpdateAppointmentStatusDto
	{
		[Required]
		public string Status { get; set; } // e.g., "Confirmed", "Cancelled", "Completed"
	}
}
