namespace DoctorAppointmentSystem.Domain.Exceptions
{
	public class DoctorNotFoundException : NotFoundException
	{
		public DoctorNotFoundException(Guid doctorId)
			: base($"Doctor with ID '{doctorId}' was not found.", "Doctor Not Found")
		{
		}
	}

	public class DoctorNotVerifiedException : ForbiddenException
	{
		public DoctorNotVerifiedException(Guid doctorId)
			: base($"Doctor with ID '{doctorId}' is not verified by administration.", "Doctor Not Verified")
		{
		}
	}
}
