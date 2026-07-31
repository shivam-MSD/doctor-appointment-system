using System;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace DoctorAppointmentSystem.Application.Services
{
	public class HangfireJobService : IHangfireJobService
	{
		private readonly IBackgroundJobClient _backgroundJobClient;
		private readonly ILogger<HangfireJobService> _logger;

		public HangfireJobService(IBackgroundJobClient backgroundJobClient, ILogger<HangfireJobService> logger)
		{
			_backgroundJobClient = backgroundJobClient;
			_logger = logger;
		}

		public void EnqueueAppointmentEmail(
			string toEmail,
			string subject,
			string title,
			string message,
			string doctorName,
			string dateStr,
			string timeOrStatus,
			string clinicName,
			string clinicAddress,
			string? patientName = null,
			string? comment = null,
			string? report = null,
			string? followUpStr = null)
		{
			_backgroundJobClient.Enqueue<IAppointmentService>(appService =>
				appService.SendAppointmentEmailAsync(
					toEmail, subject, title, message, doctorName, dateStr, timeOrStatus,
					null, patientName, comment, report, followUpStr, null, null
				)
			);
		}

		public void EnqueueNotification(string userId, string message)
		{
			if (Guid.TryParse(userId, out var userGuid))
			{
				_backgroundJobClient.Enqueue<INotificationService>(notifService =>
					notifService.CreateNotificationAsync(userGuid, message)
				);
			}
		}
	}
}
