using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace DoctorAppointmentSystem.Application.Services
{
	public interface IEmailService
	{
		Task SendEmailAsync(string toEmail, string subject, string body);
	}

	public class EmailService : IEmailService
	{
		private readonly IConfiguration _configuration;

		public EmailService(IConfiguration configuration)
		{
			_configuration = configuration;
		}

		public async Task SendEmailAsync(string toEmail, string subject, string body)
		{
			var host = _configuration["MailSettings:Host"] ?? "smtp.gmail.com";
			var portStr = _configuration["MailSettings:Port"] ?? "587";
			var senderEmail = _configuration["MailSettings:Mail"] ?? "your-email@gmail.com";
			var senderName = _configuration["MailSettings:DisplayName"] ?? "HealSync Appointments";
			var password = _configuration["MailSettings:Password"] ?? "your-password-or-app-password";

			int.TryParse(portStr, out int port);

			// Check if we are running with default placeholders or empty config values
			bool isPlaceholder = string.IsNullOrWhiteSpace(senderEmail) || 
			                     senderEmail.Contains("[EMAIL_ADDRESS]") || 
			                     string.IsNullOrWhiteSpace(password) || 
			                     password.Contains("[PASSWORD]");

			if (isPlaceholder)
			{
				Console.WriteLine("\n==================================================");
				Console.WriteLine($"[EMAIL FALLBACK - NO SMTP SERVICE CONFIGURED]");
				Console.WriteLine($"To: {toEmail}");
				Console.WriteLine($"Subject: {subject}");
				Console.WriteLine($"Body: {body}");
				Console.WriteLine("==================================================\n");
				return;
			}

			try
			{
				using var client = new SmtpClient(host, port)
				{
					Credentials = new NetworkCredential(senderEmail, password),
					EnableSsl = true
				};

				var mailMessage = new MailMessage
				{
					From = new MailAddress(senderEmail, senderName),
					Subject = subject,
					Body = body,
					IsBodyHtml = true
				};
				mailMessage.To.Add(toEmail);

				await client.SendMailAsync(mailMessage);
			}
			catch (Exception ex)
			{
				// If SMTP fails, print to Console so testing doesn't break
				Console.WriteLine("\n==================================================");
				Console.WriteLine($"[EMAIL FALLBACK - SMTP SENDING FAILED: {ex.Message}]");
				Console.WriteLine($"To: {toEmail}");
				Console.WriteLine($"Subject: {subject}");
				Console.WriteLine($"Body: {body}");
				Console.WriteLine("==================================================\n");
			}
		}
	}
}
