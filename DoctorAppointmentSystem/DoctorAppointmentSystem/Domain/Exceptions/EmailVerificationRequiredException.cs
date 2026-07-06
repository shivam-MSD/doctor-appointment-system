using System.Net;

namespace DoctorAppointmentSystem.Domain.Exceptions
{
	public class EmailVerificationRequiredException : BaseException
	{
		public EmailVerificationRequiredException(string email)
			: base($"EmailVerificationRequired: Verification OTP has been sent to {email}. Please verify your email before logging in.",
				   HttpStatusCode.Forbidden,
				   "Email Verification Required")
		{
		}
	}
}
