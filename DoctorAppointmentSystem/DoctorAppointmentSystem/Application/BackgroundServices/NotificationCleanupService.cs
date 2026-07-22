using Microsoft.EntityFrameworkCore;
using DoctorAppointmentSystem.Persistent.Context;

namespace DoctorAppointmentSystem.Application.BackgroundServices
{
	public class NotificationCleanupService : BackgroundService
	{
		private readonly IServiceProvider _serviceProvider;
		private readonly ILogger<NotificationCleanupService> _logger;

		public NotificationCleanupService(IServiceProvider serviceProvider, ILogger<NotificationCleanupService> logger)
		{
			_serviceProvider = serviceProvider;
			_logger = logger;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			// Run immediately on startup, then every 24 hours
			while (!stoppingToken.IsCancellationRequested)
			{
				try
				{
					_logger.LogInformation("NotificationCleanupService is running.");

					using (var scope = _serviceProvider.CreateScope())
					{
						var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

						// Define the retention period (e.g., 30 days)
						var retentionDate = DateTime.UtcNow.AddDays(-30);

						// Only delete notifications that are Read AND older than 30 days
						var oldReadNotifications = await dbContext.Notifications
							.Where(n => n.IsRead == true && n.CreatedDate < retentionDate)
							.ToListAsync(stoppingToken);

						if (oldReadNotifications.Any())
						{
							dbContext.Notifications.RemoveRange(oldReadNotifications);
							await dbContext.SaveChangesAsync(stoppingToken);
							_logger.LogInformation($"Successfully deleted {oldReadNotifications.Count} old read notifications.");
						}
						else
						{
							_logger.LogInformation("No old read notifications to clean up.");
						}
					}
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error occurred executing NotificationCleanupService.");
				}

				// Wait for 24 hours before running again
				await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
			}
		}
	}
}
