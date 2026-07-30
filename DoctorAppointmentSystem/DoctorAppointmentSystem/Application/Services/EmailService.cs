using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;

namespace DoctorAppointmentSystem.Application.Services
{
	#region Event Models

	/// <summary>
	/// Event arguments used when an email is raised through the event system.
	/// </summary>
	public class EmailSendEventArgs : EventArgs
	{
		public Guid? UserId { get; set; }

		public string FirstName { get; set; }

		public string LastName { get; set; }

		public string Email { get; set; } = string.Empty;

		public string Subject { get; set; } = string.Empty;

		public string Body { get; set; } = string.Empty;

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	}

	/// <summary>
	/// Delegate used for asynchronous email sending.
	/// </summary>
	public delegate void EmailSendEventHandler(object? sender, EmailSendEventArgs e);

	#endregion

	#region Interface

	public interface IEmailService
	{
		event EmailSendEventHandler EmailSendEvent;

		Task SendEmailAsync(string toEmail, string subject, string body);

		Task SendOtpVerificationEmailAsync(string toEmail,string firstName,string lastName,string otp,int expiryMinutes = 15);

		Task SendPasswordResetEmailAsync(string toEmail,string firstName,string lastName,string otp);

		Task SendAppointmentConfirmationAsync(string toEmail,string firstNam,string lastName,string appointmentDetails);

		Task SendAppointmentCancellationAsync(string toEmail,string firstName,string lastName,string appointmentDetails,string reason);

		Task SendDoctorVerificationEmailAsync(string toEmail,string firstName,string lastName,string doctorName,bool isApproved,string? rejectionReason = null);

		Task SendClinicVerificationEmailAsync(string toEmail,string adminFirstName,string adminLastName,string clinicName,bool isApproved,string? rejectionReason = null);

		Task SendDoctorOnboardingReceivedEmailAsync(string toEmail, string firstName, string lastName);
	}

	#endregion

	public class EmailService : IEmailService
	{
		private readonly IConfiguration _configuration;

		public event EmailSendEventHandler? EmailSendEvent;

		public EmailService(IConfiguration configuration)
		{
			_configuration = configuration;

			EmailSendEvent += OnEmailSendEvent;
		}

		/// <summary>
		/// Public method used by all services.
		/// It raises an event so the caller doesn't wait for SMTP.
		/// </summary>
		public async Task SendEmailAsync(
			string toEmail,
			string subject,
			string body)
		{
			if (string.IsNullOrWhiteSpace(toEmail))
				throw new ArgumentException(nameof(toEmail));

			if (string.IsNullOrWhiteSpace(subject))
				throw new ArgumentException(nameof(subject));

			if (string.IsNullOrWhiteSpace(body))
				throw new ArgumentException(nameof(body));

			await SendEmailThroughEventAsync(toEmail, subject, body);
		}

		/// <summary>
		/// Fire-and-forget email sending.
		/// </summary>
		private Task SendEmailThroughEventAsync(
			string toEmail,
			string subject,
			string body)
		{
			RaiseEmailSendEvent(toEmail, subject, body);

			return Task.CompletedTask;
		}

		private void RaiseEmailSendEvent(
			string email,
			string subject,
			string body)
		{
			EmailSendEvent?.Invoke(this, new EmailSendEventArgs
			{
				Email = email,
				Subject = subject,
				Body = body
			});
		}

		/// <summary>
		/// Event subscriber.
		/// Runs on a background thread.
		/// </summary>
		private void OnEmailSendEvent(
			object? sender,
			EmailSendEventArgs e)
		{
			Task.Run(async () =>
			{
				try
				{
					await SendEmailInternalAsync(
						e.Email,
						e.Subject,
						e.Body);
				}
				catch (Exception ex)
				{
					Console.WriteLine($"[EMAIL ERROR] {ex.Message}");
				}
			});
		}

		/// <summary>
		/// Actual SMTP implementation.
		/// </summary>
		private async Task SendEmailInternalAsync(
			string toEmail,
			string subject,
			string body)
		{
			var host = _configuration["MailSettings:Host"];
			var port = Convert.ToInt32(_configuration["MailSettings:Port"]);
			var senderEmail = _configuration["MailSettings:Mail"];
			var senderName = _configuration["MailSettings:DisplayName"];
			var password = _configuration["MailSettings:Password"];

			using var client = new SmtpClient(host, port)
			{
				EnableSsl = true,
				Credentials = new NetworkCredential(
					senderEmail,
					password)
			};

			var mail = new MailMessage
			{
				From = new MailAddress(
					senderEmail!,
					senderName),

				Subject = subject,

				Body = body,

				IsBodyHtml = true
			};

			mail.To.Add(toEmail);

			await client.SendMailAsync(mail);
		}

		/// <summary>
		/// Creates the common HTML template used by every email.
		/// Only the title, color and body content change.
		/// </summary>
		private static string BuildTemplate(
			string title,
			string titleColor,
			string content,
			string portalUrl = "https://healsync-medical.web.app/patient/dashboard",
			string buttonText = "View Details &rarr;")
		{
			return $@"
<div style=""font-family: 'Segoe UI', Arial, sans-serif; background-color: #f1f5f9; padding: 40px 10px; margin: 0;"">
  <div style=""max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 10px 25px rgba(0, 0, 0, 0.05); border: 1px solid #e2e8f0;"">
    <div style=""background: linear-gradient(135deg, #0e7490, #0891b2); padding: 28px 24px; text-align: center;"">
      <h1 style=""color: #ffffff; margin: 0; font-size: 22px; font-weight: 700; letter-spacing: 0.5px;"">HealSync Medical Network</h1>
    </div>
    <div style=""padding: 32px 28px; color: #1f2937;"">
      <h2 style=""color: {titleColor}; margin-top: 0; margin-bottom: 16px; font-size: 20px; font-weight: 600;"">{title}</h2>
      <div style=""font-size: 15px; line-height: 1.6; color: #475569; margin-bottom: 24px;"">
        {content}
      </div>
      <div style=""text-align: center; margin-top: 28px; margin-bottom: 8px;"">
        <a href=""{portalUrl}"" style=""background-color: #06b6d4; color: #ffffff; text-decoration: none; padding: 12px 28px; border-radius: 8px; font-weight: 600; font-size: 14px; display: inline-block;"">
          {buttonText}
        </a>
      </div>
    </div>
    <div style=""background-color: #f8fafc; border-top: 1px solid #e2e8f0; padding: 18px 24px; text-align: center;"">
      <p style=""margin: 0; font-size: 12px; color: #94a3b8; line-height: 1.5;"">
        This is an automated notification from HealSync Medical Network.<br/>Please do not reply directly to this email.
      </p>
    </div>
  </div>
</div>";
		}
		/// <summary>
		/// Sends OTP verification email.
		/// </summary>
		public async Task SendOtpVerificationEmailAsync(
	string toEmail,
	string firstName,
	string lastName,
	string otp,
	int expiryMinutes = 15)
		{
			var subject = "HealSync - Email Verification";

			var content = $@"
<p>Hello,</p>

<p>
Thank you for signing up with <strong>HealSync</strong>.
Please use the following One-Time Password (OTP) to verify your email address.
</p>

<div style='text-align:center;margin:35px 0;'>

<span style='font-size:32px;
font-weight:bold;
letter-spacing:8px;
padding:12px 26px;
background:#f3f4f6;
border:1px solid #d1d5db;
border-radius:8px;'>

{otp}

</span>

</div>

<p>
This OTP is valid for
<strong>{expiryMinutes} minutes</strong>.
</p>

<p style='color:#ef4444'>
Do not share this OTP with anyone.
</p>";

			var body = BuildTemplate(
				"Email Verification",
				"#06b6d4",
				content);

			await SendEmailAsync(toEmail, subject, body);
		}

		/// <summary>
		/// Sends password reset OTP.
		/// </summary>
		public async Task SendPasswordResetEmailAsync(
			string toEmail,
			string firstName,
			string lastName,
			string otp)
		{
			var subject = "HealSync - Password Reset";

			var content = $@"
<p>Hello,</p>

<p>
We received a request to reset your password.
Please use the following OTP.
</p>

<div style='text-align:center;margin:35px 0;'>

<span style='font-size:32px;
font-weight:bold;
letter-spacing:8px;
padding:12px 26px;
background:#fef2f2;
border:1px solid #fecaca;
border-radius:8px;
color:#dc2626;'>

{otp}

</span>

</div>

<p>
This OTP will expire in
<strong>15 minutes</strong>.
</p>

<p>
If you did not request a password reset,
you may safely ignore this email.
</p>";

			var body = BuildTemplate(
				"Password Reset",
				"#ef4444",
				content);

			await SendEmailAsync(toEmail, subject, body);
		}

		/// <summary>
		/// Sends appointment confirmation email.
		/// </summary>
		public async Task SendAppointmentConfirmationAsync(
			string toEmail,
			string firstName,
			string lastName,
			string appointmentDetails)
		{
			var subject = "HealSync - Appointment Confirmed";

			var content = $@"
<p>Hello,</p>

<p>
Your appointment has been successfully confirmed.
</p>

<div style='background:#ecfdf5;
padding:18px;
border-radius:8px;
border:1px solid #bbf7d0;
margin:20px 0;'>

{appointmentDetails}

</div>

<p>
Please arrive at least
<strong>15 minutes</strong>
before your appointment.
</p>

<p>
Thank you for choosing HealSync.
</p>";

			var body = BuildTemplate(
				"Appointment Confirmed",
				"#16a34a",
				content);

			await SendEmailAsync(toEmail, subject, body);
		}

		/// <summary>
		/// Sends appointment cancellation email.
		/// </summary>
		public async Task SendAppointmentCancellationAsync(
			string toEmail,
			string firstName,
			string lastName,
			string appointmentDetails,
			string reason)
		{
			var subject = "HealSync - Appointment Cancelled";

			var content = $@"
<p>Hello,</p>

<p>
We regret to inform you that your appointment has been cancelled.
</p>

<div style='background:#fef2f2;
padding:18px;
border-radius:8px;
border:1px solid #fecaca;
margin:20px 0;'>

{appointmentDetails}

</div>

<p>

<b>Reason:</b>

<br/><br/>

{reason}

</p>

<p>
Please log in to HealSync to book another appointment.
</p>";

			var body = BuildTemplate(
				"Appointment Cancelled",
				"#dc2626",
				content);

			await SendEmailAsync(toEmail, subject, body);
		}

		/// <summary>
		/// Sends doctor verification email.
		/// </summary>
		public async Task SendDoctorVerificationEmailAsync(
			string toEmail,
			string firstName,
			string lastName,
			string doctorName,
			bool isApproved,
			string? rejectionReason = null)
		{
			string subject;
			string title;
			string color;
			string content;

			if (isApproved)
			{
				subject = "HealSync - Doctor Profile Approved";

				title = "Congratulations Doctor!";

				color = "#16a34a";

				content = $@"
<p>

Dear Dr. <b>{doctorName}</b>,

</p>

<p>

Congratulations!

</p>

<p>

Your profile has been successfully verified by the HealSync Administration Team.

</p>

<p>

You can now log in and begin accepting appointments from patients.

</p>

<p>

Welcome to HealSync!

</p>";
			}
			else
			{
				subject = "HealSync - Doctor Profile Rejected";

				title = "Verification Failed";

				color = "#f59e0b";

				content = $@"
<p>

Dear Dr. <b>{doctorName}</b>,

</p>

<p>

Unfortunately, we could not approve your profile verification.

</p>

<p>

<b>Reason</b>

</p>

<div style='background:#fffbeb;
padding:15px;
border:1px solid #fde68a;
border-radius:8px;'>

{rejectionReason}

</div>

<p>

You may update your information and submit your profile again.

</p>";
			}

			var body = BuildTemplate(title, color, content, "https://healsync-medical.web.app/doctor/dashboard", "Open Doctor Portal &rarr;");

			await SendEmailAsync(toEmail, subject, body);
		}

		/// <summary>
		/// Sends clinic verification email.
		/// </summary>
		public async Task SendClinicVerificationEmailAsync(
			string toEmail,
			string adminFirstName,
			string adminLastName,
			string clinicName,
			bool isApproved,
			string? rejectionReason = null)
		{
			string subject;
			string title;
			string color;
			string content;

			if (isApproved)
			{
				subject = "HealSync - Clinic Approved";

				title = "Clinic Approved";

				color = "#16a34a";

				content = $@"
<p>

Congratulations!

</p>

<p>

Your clinic

<b>{clinicName}</b>

has been approved successfully.

</p>

<p>

Patients can now book appointments for this clinic.

</p>";
			}
			else
			{
				subject = "HealSync - Clinic Rejected";

				title = "Clinic Rejected";

				color = "#f59e0b";

				content = $@"
<p>

Unfortunately,

your clinic

<b>{clinicName}</b>

could not be approved.

</p>

<p>

<b>Reason</b>

</p>

<div style='background:#fffbeb;
padding:15px;
border-radius:8px;
border:1px solid #fde68a;'>

{rejectionReason}

</div>

<p>

Please correct the issues and submit the clinic again.

</p>";
			}

			var body = BuildTemplate(title, color, content, "https://healsync-medical.web.app/admin/dashboard", "Access Admin Console &rarr;");

			await SendEmailAsync(toEmail, subject, body);
		}

		public async Task SendDoctorOnboardingReceivedEmailAsync(
			string toEmail,
			string firstName,
			string lastName)
		{
			var subject = "HealSync - Doctor Onboarding Application Received";
			var content = $@"
<p>Hello Dr. {firstName} {lastName},</p>
<p>Thank you for verifying your email address.</p>
<p>We have successfully received your medical onboarding application. Our administration team is currently verifying your credentials and medical licensing details.</p>
<p>Once approved, your secure temporary password will be sent to this email address within 24-48 hours. You will then be able to log in and update your password.</p>
<p>Best regards,<br/>HealSync Administration Team</p>";

			var body = BuildTemplate(
				"Application Received",
				"#06b6d4",
				content,
				"https://healsync-medical.web.app/doctor/dashboard",
				"Open Doctor Portal &rarr;");

			await SendEmailAsync(toEmail, subject, body);
		}
	}
}