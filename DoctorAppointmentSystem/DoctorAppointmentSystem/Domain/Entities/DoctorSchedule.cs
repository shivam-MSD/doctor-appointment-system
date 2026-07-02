using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoctorAppointmentSystem.Domain.Entities
{
	[Table("DoctorSchedules")]
	public class DoctorSchedule
	{
		[Key]
		public Guid ScheduleId { get; set; }

		[Required]
		public Doctor Doctor { get; set; }

		[Required]
		public TimeSpan StartTime { get; set; }

		[Required]
		public TimeSpan EndTime { get; set; }

		[Required]
		public bool IsAvailable { get; set; }

		[Required]
		public int DaysOfWeek { get; set; }
	}
}
