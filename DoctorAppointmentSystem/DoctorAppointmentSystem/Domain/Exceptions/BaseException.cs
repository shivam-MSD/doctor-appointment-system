using System.Net;

namespace DoctorAppointmentSystem.Domain.Exceptions
{
	public abstract class BaseException : Exception
	{
		public HttpStatusCode StatusCode { get; }
		public string Title { get; }

		protected BaseException(string message, HttpStatusCode statusCode, string title)
			: base(message)
		{
			StatusCode = statusCode;
			Title = title;
		}
	}

	public abstract class NotFoundException : BaseException
	{
		protected NotFoundException(string message, string title = "Not Found")
			: base(message, HttpStatusCode.NotFound, title)
		{
		}
	}

	public abstract class BadRequestException : BaseException
	{
		protected BadRequestException(string message, string title = "Bad Request")
			: base(message, HttpStatusCode.BadRequest, title)
		{
		}
	}

	public abstract class ConflictException : BaseException
	{
		protected ConflictException(string message, string title = "Conflict")
			: base(message, HttpStatusCode.Conflict, title)
		{
		}
	}

	public abstract class UnauthorizedException : BaseException
	{
		protected UnauthorizedException(string message, string title = "Unauthorized")
			: base(message, HttpStatusCode.Unauthorized, title)
		{
		}
	}

	public abstract class ForbiddenException : BaseException
	{
		protected ForbiddenException(string message, string title = "Forbidden")
			: base(message, HttpStatusCode.Forbidden, title)
		{
		}
	}
}
