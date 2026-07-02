namespace DoctorAppointmentSystem.Domain.Exceptions
{
	public class PatientNotFoundException : NotFoundException
	{
		public PatientNotFoundException(Guid patientId)
			: base($"Patient with ID '{patientId}' was not found.", "Patient Not Found")
		{
		}
	}
}
