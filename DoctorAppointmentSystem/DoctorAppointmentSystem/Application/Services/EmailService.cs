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

			using var client = new SmtpClient(host, port)
			{
				Credentials = new NetworkCredential(senderEmail, password),
				EnableSsl = true
			};

			var mailMessage = new MailMessage
			{
				From = new MailAddress(senderEmail, senderName),
				Subject = senderName,
				Body = body,
				IsBodyHtml = true
			};
			mailMessage.To.Add(toEmail);

			await client.SendMailAsync(mailMessage);
		}
	}
}
