using System.Net;

namespace DoctorAppointmentSystem.Domain.Exceptions
{
	public class BaseException : Exception
	{
		public HttpStatusCode StatusCode { get; }
		public string Title { get; }

		public BaseException(string message, HttpStatusCode statusCode, string title)
			: base(message)
		{
			StatusCode = statusCode;
			Title = title;
		}
	}

	public class NotFoundException : BaseException
	{
		public NotFoundException(string message, string title = "Not Found")
			: base(message, HttpStatusCode.NotFound, title)
		{
		}
	}

	public class BadRequestException : BaseException
	{
		public BadRequestException(string message, string title = "Bad Request")
			: base(message, HttpStatusCode.BadRequest, title)
		{
		}
	}

	public class ConflictException : BaseException
	{
		public ConflictException(string message, string title = "Conflict")
			: base(message, HttpStatusCode.Conflict, title)
		{
		}
	}

	public class UnauthorizedException : BaseException
	{
		public UnauthorizedException(string message, string title = "Unauthorized")
			: base(message, HttpStatusCode.Unauthorized, title)
		{
		}
	}

	public class ForbiddenException : BaseException
	{
		public ForbiddenException(string message, string title = "Forbidden")
			: base(message, HttpStatusCode.Forbidden, title)
		{
		}
	}
}
