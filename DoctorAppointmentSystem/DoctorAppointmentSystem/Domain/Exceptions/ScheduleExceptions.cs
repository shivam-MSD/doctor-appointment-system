namespace DoctorAppointmentSystem.Domain.Exceptions
{
	public class ScheduleNotFoundException : NotFoundException
	{
		public ScheduleNotFoundException(Guid scheduleId)
			: base($"Schedule with ID '{scheduleId}' was not found.", "Schedule Not Found")
		{
		}
	}

	public class ScheduleConflictException : ConflictException
	{
		public ScheduleConflictException(TimeSpan startTime, TimeSpan endTime)
			: base($"The schedule slot {startTime:hh\\:mm} - {endTime:hh\\:mm} conflicts with an existing schedule slot.", "Schedule Conflict")
		{
		}
	}

	public class InvalidTimeRangeException : BadRequestException
	{
		public InvalidTimeRangeException(string message = "The start time must be before the end time.")
			: base(message, "Invalid Time Range")
		{
		}
	}
}
