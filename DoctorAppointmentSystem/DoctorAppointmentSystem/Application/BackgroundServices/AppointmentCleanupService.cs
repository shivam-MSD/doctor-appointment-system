using DoctorAppointmentSystem.Domain.Entities;
using DoctorAppointmentSystem.Persistent.Context;
using Microsoft.EntityFrameworkCore;

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
			var emailService = scope.ServiceProvider.GetRequiredService<DoctorAppointmentSystem.Application.Services.IEmailService>();

			var today = DateTime.Today;

			// Find appointments that are older than today and are still Pending or Confirmed
			var pastAppointments = await dbContext.Appointments
				.Include(a => a.Patient)
				.Include(a => a.Doctor)
				.Include(a => a.Clinic)
					.ThenInclude(c => c.Address)
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
				var userPatient = await dbContext.UserPatients
					.Include(up => up.User)
					.FirstOrDefaultAsync(up => up.PatientId == app.Patient.PatientId, stoppingToken);

				var docName = $"{app.Doctor.FirstName} {app.Doctor.LastName}";
				var patientFullName = $"{app.Patient.FirstName} {app.Patient.LastName}";
				var dateStr = app.AppointmentDate.ToString("dd MMM yyyy");

				if (app.EAppointmentStatus == EAppointmentStatus.Confirmed)
				{
					app.EAppointmentStatus = EAppointmentStatus.Completed;
					app.Comment = (string.IsNullOrWhiteSpace(app.Comment) ? "" : app.Comment + " | ") + "System Auto-Completed";
					completedCount++;
					madeChanges = true;

					if (userPatient != null)
					{
						await notificationService.CreateNotificationAsync(userPatient.UserId, $"Your appointment with Dr. {docName} on {dateStr} has been automatically marked as Completed.");

						if (userPatient.User != null && !string.IsNullOrWhiteSpace(userPatient.User.Email))
						{
							var subject = "HealSync - Appointment Auto-Completed";
							var title = "Appointment Auto-Completed";
							var msg = $"Dear {patientFullName}, your confirmed appointment with Dr. {docName} on {dateStr} has been automatically marked as Completed by the system.";
							
							await SendCleanupEmailAsync(emailService, userPatient.User.Email, subject, title, msg, docName, dateStr, "Completed", app.Clinic, patientFullName);
						}
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
						await notificationService.CreateNotificationAsync(userPatient.UserId, $"Your waitlisted appointment with Dr. {docName} on {dateStr} has expired and was automatically Cancelled.");

						if (userPatient.User != null && !string.IsNullOrWhiteSpace(userPatient.User.Email))
						{
							var subject = "HealSync - Appointment Auto-Cancelled (Expired Waitlist)";
							var title = "Appointment Auto-Cancelled";
							var msg = $"Dear {patientFullName}, your pending waitlist request for Dr. {docName} on {dateStr} has expired as no time slot was assigned, and has been automatically Cancelled.";
							
							await SendCleanupEmailAsync(emailService, userPatient.User.Email, subject, title, msg, docName, dateStr, "Cancelled (Expired)", app.Clinic, patientFullName, cancelledBy: "System (Auto-Cleanup)", cancelReason: "Waitlist time slot not assigned before day end");
						}
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

		private static async Task SendCleanupEmailAsync(
			DoctorAppointmentSystem.Application.Services.IEmailService emailService,
			string toEmail,
			string subject,
			string title,
			string message,
			string doctorName,
			string dateStr,
			string statusStr,
			Clinic? clinic,
			string patientName,
			string? cancelledBy = null,
			string? cancelReason = null)
		{
			string clinicName = clinic?.ClinicName ?? "N/A";
			string clinicAddress = "N/A";
			if (clinic != null)
			{
				var parts = new List<string>();
				if (!string.IsNullOrWhiteSpace(clinic.Address?.Addressline1)) parts.Add(clinic.Address.Addressline1);
				if (!string.IsNullOrWhiteSpace(clinic.Address?.Addressline2)) parts.Add(clinic.Address.Addressline2);
				if (!string.IsNullOrWhiteSpace(clinic.Address?.Area)) parts.Add(clinic.Address.Area);
				if (!string.IsNullOrWhiteSpace(clinic.Address?.City)) parts.Add(clinic.Address.City);
				if (!string.IsNullOrWhiteSpace(clinic.Address?.State)) parts.Add(clinic.Address.State);
				if (!string.IsNullOrWhiteSpace(clinic.Address?.Pincode)) parts.Add(clinic.Address.Pincode);
				clinicAddress = string.Join(", ", parts);
			}

			var extraRowsHtml = new System.Text.StringBuilder();

			if (!string.IsNullOrWhiteSpace(cancelledBy))
			{
				extraRowsHtml.Append($@"
          <tr>
            <td style=""padding: 6px 0; color: #dc2626; font-weight: 600; width: 35%;"">Cancelled By:</td>
            <td style=""padding: 6px 0; color: #dc2626; font-weight: 700;"">{cancelledBy}</td>
          </tr>");
			}

			if (!string.IsNullOrWhiteSpace(cancelReason))
			{
				extraRowsHtml.Append($@"
          <tr>
            <td style=""padding: 6px 0; color: #dc2626; font-weight: 500; vertical-align: top;"">Reason:</td>
            <td style=""padding: 6px 0; color: #dc2626; font-weight: 600;"">{cancelReason}</td>
          </tr>");
			}

			string htmlBody = $@"
<div style=""font-family: 'Segoe UI', Arial, sans-serif; background-color: #f3f4f6; padding: 40px 10px; margin: 0;"">
  <div style=""max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 12px rgba(0, 0, 0, 0.05); border: 1px solid #e5e7eb;"">
    <div style=""background: linear-gradient(135deg, #0e7490, #0891b2); padding: 30px 20px; text-align: center;"">
      <h1 style=""color: #ffffff; margin: 0; font-size: 24px; font-weight: 700; letter-spacing: 0.5px;"">HealSync Appointments</h1>
    </div>
    <div style=""padding: 40px 30px; color: #1f2937;"">
      <h2 style=""color: #0e7490; margin-top: 0; margin-bottom: 16px; font-size: 20px; font-weight: 600;"">{title}</h2>
      <p style=""font-size: 16px; line-height: 1.6; color: #4b5563; margin-bottom: 24px;"">{message}</p>
      
      <div style=""background-color: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; padding: 20px; margin-bottom: 24px;"">
        <table style=""width: 100%; border-collapse: collapse; font-size: 15px;"">
          <tr>
            <td style=""padding: 6px 0; color: #64748b; font-weight: 500; width: 35%;"">Patient Name:</td>
            <td style=""padding: 6px 0; color: #0f172a; font-weight: 600;"">{patientName}</td>
          </tr>
          <tr>
            <td style=""padding: 6px 0; color: #64748b; font-weight: 500;"">Doctor:</td>
            <td style=""padding: 6px 0; color: #0f172a; font-weight: 600;"">Dr. {doctorName}</td>
          </tr>
          <tr>
            <td style=""padding: 6px 0; color: #64748b; font-weight: 500;"">Appointment Date:</td>
            <td style=""padding: 6px 0; color: #0f172a; font-weight: 600;"">{dateStr}</td>
          </tr>
          <tr>
            <td style=""padding: 6px 0; color: #64748b; font-weight: 500;"">Status:</td>
            <td style=""padding: 6px 0; color: #0f172a; font-weight: 600;"">{statusStr}</td>
          </tr>
          {extraRowsHtml}
          <tr>
            <td style=""padding: 6px 0; color: #64748b; font-weight: 500; vertical-align: top;"">Clinic Branch:</td>
            <td style=""padding: 6px 0; color: #0f172a; font-weight: 600; line-height: 1.4;"">{clinicName}<br/><span style=""font-weight: 400; color: #475569; font-size: 14px;"">📍 {clinicAddress}</span></td>
          </tr>
        </table>
      </div>
      
      <div style=""text-align: center; margin-top: 28px; margin-bottom: 24px;"">
        <a href=""https://healsync-medical.web.app/patient/dashboard"" style=""background-color: #06b6d4; color: #ffffff; text-decoration: none; padding: 12px 28px; border-radius: 8px; font-weight: 600; font-size: 14px; display: inline-block;"">
          View Appointment Details &rarr;
        </a>
      </div>

      <p style=""font-size: 14px; line-height: 1.5; color: #94a3b8; margin-top: 32px; border-top: 1px solid #e2e8f0; padding-top: 16px;"">This is an automated notification from HealSync. Please do not reply directly to this email.</p>
    </div>
  </div>
</div>";

			try
			{
				await emailService.SendEmailAsync(toEmail, subject, htmlBody);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[Cleanup Email Error]: {ex.Message}");
			}
		}
	}
}
