namespace DoctorAppointmentSystem.Domain.Exceptions
{
	public class AppointmentNotFoundException : NotFoundException
	{
		public AppointmentNotFoundException(Guid appointmentId)
			: base($"Appointment with ID '{appointmentId}' was not found.", "Appointment Not Found")
		{
		}
	}

	public class AppointmentSlotNotAvailableException : ConflictException
	{
		public AppointmentSlotNotAvailableException(Guid doctorId, DateTime dateTime)
			: base($"The selected time slot on {dateTime:yyyy-MM-dd HH:mm} for doctor with ID '{doctorId}' is already booked or unavailable.", "Slot Unavailable")
		{
		}
	}

	public class InvalidAppointmentStateException : BadRequestException
	{
		public InvalidAppointmentStateException(string status, string action)
			: base($"Cannot perform action '{action}' on an appointment with status '{status}'.", "Invalid Appointment State")
		{
		}
	}
}
