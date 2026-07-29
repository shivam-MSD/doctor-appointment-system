using System;

namespace DoctorAppointmentSystem.Application.Services
{
	public static class EmailSender
	{
		public static event EmailSendEventHandler? EmailSendEvent;

		public static void SendEmail(string email, string subject, string body)
		{
			EmailSendEvent?.Invoke(null, new EmailSendEventArgs
			{
				Email = email,
				Subject = subject,
				Body = body,
				CreatedAt = DateTime.UtcNow
			});
		}
	}
}
