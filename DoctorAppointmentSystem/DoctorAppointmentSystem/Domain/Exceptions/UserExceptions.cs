namespace DoctorAppointmentSystem.Domain.Exceptions
{
	public class UserNotFoundException : NotFoundException
	{
		public UserNotFoundException(Guid userId)
			: base($"User with ID '{userId}' was not found.", "User Not Found")
		{
		}

		public UserNotFoundException(string email)
			: base($"User with email '{email}' was not found.", "User Not Found")
		{
		}
	}

	public class EmailAlreadyExistsException : ConflictException
	{
		public EmailAlreadyExistsException(string email)
			: base($"A user with email '{email}' already exists.", "Email Already Registered")
		{
		}
	}

	public class InvalidCredentialsException : BadRequestException
	{
		public InvalidCredentialsException()
			: base("The email or password provided is incorrect.", "Invalid Credentials")
		{
		}
	}
}
