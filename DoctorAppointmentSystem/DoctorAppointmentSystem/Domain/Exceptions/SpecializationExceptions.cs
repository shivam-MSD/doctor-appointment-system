namespace DoctorAppointmentSystem.Domain.Exceptions
{
	public class SpecializationNotFoundException : NotFoundException
	{
		public SpecializationNotFoundException(Guid specializationId)
			: base($"Specialization with ID '{specializationId}' was not found.", "Specialization Not Found")
		{
		}
	}
}
