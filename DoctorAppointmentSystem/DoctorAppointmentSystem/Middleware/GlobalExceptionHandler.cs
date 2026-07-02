using System.Net;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using DoctorAppointmentSystem.Domain.Exceptions;

namespace DoctorAppointmentSystem.Middleware
{
	public class GlobalExceptionHandler : IExceptionHandler
	{
		private readonly ILogger<GlobalExceptionHandler> _logger;

		public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
		{
			_logger = logger;
		}

		public async ValueTask<bool> TryHandleAsync(
			HttpContext httpContext,
			Exception exception,
			CancellationToken cancellationToken)
		{
			_logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);

			var problemDetails = new ProblemDetails
			{
				Instance = httpContext.Request.Path
			};

			if (exception is BaseException baseException)
			{
				problemDetails.Status = (int)baseException.StatusCode;
				problemDetails.Title = baseException.Title;
				problemDetails.Detail = baseException.Message;
			}
			else
			{
				problemDetails.Status = (int)HttpStatusCode.InternalServerError;
				problemDetails.Title = "Internal Server Error";
				problemDetails.Detail = "An unexpected error occurred. Please try again later.";
			}

			httpContext.Response.StatusCode = problemDetails.Status.Value;
			httpContext.Response.ContentType = "application/problem+json";

			await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

			return true;
		}
	}
}
