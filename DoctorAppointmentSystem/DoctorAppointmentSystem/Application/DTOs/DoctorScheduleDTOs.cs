using System.ComponentModel.DataAnnotations;

namespace DoctorAppointmentSystem.Application.DTOs
{
	public class DoctorScheduleDto
	{
		public Guid ScheduleId { get; set; }
		public Guid DoctorId { get; set; }
		public TimeSpan StartTime { get; set; }
		public TimeSpan EndTime { get; set; }
		public bool IsAvailable { get; set; }
		public int DaysOfWeek { get; set; }
	}

	public class CreateScheduleDto
	{
		[Required]
		public Guid DoctorId { get; set; }

		[Required]
		public TimeSpan StartTime { get; set; }

		[Required]
		public TimeSpan EndTime { get; set; }

		[Required]
		public int DaysOfWeek { get; set; }

		public bool IsAvailable { get; set; } = true;
	}
}
