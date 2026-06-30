namespace DoctorAppointmentSystem.Domain.Entities
{
	public class DoctorSchedule
	{
		public Guid ScheduleId { get; set; }
		public Guid DoctorId { get; set; }
		public TimeSpan StartTime { get; set; }
		public TimeSpan EndTime { get; set; }
		public bool IsAvailable { get; set; }
		public int DaysOfWeek { get; set; }

	}
}
