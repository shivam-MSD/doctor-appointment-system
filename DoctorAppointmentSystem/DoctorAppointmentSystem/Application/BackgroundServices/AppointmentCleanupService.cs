using DoctorAppointmentSystem.Domain.Entities;
using DoctorAppointmentSystem.Persistent.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DoctorAppointmentSystem.Application.BackgroundServices
{
	public class AppointmentCleanupService : BackgroundService
	{
		private readonly IServiceProvider _serviceProvider;
		private readonly ILogger<AppointmentCleanupService> _logger;

		public AppointmentCleanupService(IServiceProvider serviceProvider, ILogger<AppointmentCleanupService> logger)
		{
			_serviceProvider = serviceProvider;
			_logger = logger;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			_logger.LogInformation("Appointment Cleanup Service is starting.");

			// For demonstration/testing, run every 1 minute.
			// In production, this would be TimeSpan.FromHours(24) or run at a specific time.
			var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));

			while (await timer.WaitForNextTickAsync(stoppingToken))
			{
				try
				{
					await CleanupAppointmentsAsync(stoppingToken);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error occurred executing Appointment Cleanup.");
				}
			}
		}

		private async Task CleanupAppointmentsAsync(CancellationToken stoppingToken)
		{
			_logger.LogInformation("Appointment Cleanup running at: {time}", DateTimeOffset.Now);

			using var scope = _serviceProvider.CreateScope();
			var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
			var notificationService = scope.ServiceProvider.GetRequiredService<DoctorAppointmentSystem.Application.Services.INotificationService>();

			var today = DateTime.Today;

			// Find appointments that are older than today and are still Pending or Confirmed
			var pastAppointments = await dbContext.Appointments
				.Include(a => a.Patient)
				.Include(a => a.Doctor)
				.Where(a => a.AppointmentDate < today && 
							(a.EAppointmentStatus == EAppointmentStatus.Pending || 
							 a.EAppointmentStatus == EAppointmentStatus.Confirmed))
				.ToListAsync(stoppingToken);

			if (!pastAppointments.Any())
			{
				_logger.LogInformation("No past appointments found that need cleanup.");
				return;
			}

			int completedCount = 0;
			int cancelledCount = 0;
			bool madeChanges = false;

			foreach (var app in pastAppointments)
			{
				var userPatient = await dbContext.UserPatients.FirstOrDefaultAsync(up => up.PatientId == app.Patient.PatientId, stoppingToken);

				if (app.EAppointmentStatus == EAppointmentStatus.Confirmed)
				{
					app.EAppointmentStatus = EAppointmentStatus.Completed;
					app.Comment = (string.IsNullOrWhiteSpace(app.Comment) ? "" : app.Comment + " | ") + "System Auto-Completed";
					completedCount++;
					madeChanges = true;

					if (userPatient != null)
					{
						await notificationService.CreateNotificationAsync(userPatient.UserId, $"Your appointment with Dr. {app.Doctor.FirstName} {app.Doctor.LastName} on {app.AppointmentDate:MMM dd, yyyy} has been automatically marked as Completed.");
					}
				}
				else if (app.EAppointmentStatus == EAppointmentStatus.Pending)
				{
					app.EAppointmentStatus = EAppointmentStatus.Cancelled;
					app.Comment = (string.IsNullOrWhiteSpace(app.Comment) ? "" : app.Comment + " | ") + "System Auto-Cancelled (No-Show)";
					cancelledCount++;
					madeChanges = true;

					if (userPatient != null)
					{
						await notificationService.CreateNotificationAsync(userPatient.UserId, $"Your waitlisted appointment with Dr. {app.Doctor.FirstName} {app.Doctor.LastName} on {app.AppointmentDate:MMM dd, yyyy} has expired and was automatically Cancelled.");
					}
				}
			}

			if (madeChanges)
			{
				await dbContext.SaveChangesAsync(stoppingToken);
				await notificationService.SendRefreshSignalAsync("Appointments");
			}

			_logger.LogInformation($"Appointment Cleanup finished. Marked {completedCount} as Completed and {cancelledCount} as Cancelled.");
		}
	}
}
