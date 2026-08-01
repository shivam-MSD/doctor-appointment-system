using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace DoctorAppointmentSystem.Application.Services
{
	#region Event Models

	/// <summary>
	/// Event arguments used when an email is raised through the event system.
	/// </summary>
	public class EmailSendEventArgs : EventArgs
	{
		/// <summary>
		/// Gets or sets the optional associated user account ID.
		/// </summary>
		public Guid? UserId { get; set; }

		/// <summary>
		/// Gets or sets the recipient's first name.
		/// </summary>
		public string FirstName { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the recipient's last name.
		/// </summary>
		public string LastName { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the recipient's target email address.
		/// </summary>
		public string Email { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the email subject line.
		/// </summary>
		public string Subject { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the HTML body content of the email.
		/// </summary>
		public string Body { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the UTC creation timestamp when the event was raised.
		/// </summary>
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	}

	/// <summary>
	/// Delegate used for asynchronous email sending events.
	/// </summary>
	/// <param name="sender">The event source object.</param>
	/// <param name="e">The email event argument details.</param>
	public delegate void EmailSendEventHandler(object? sender, EmailSendEventArgs e);

	#endregion

	#region Interface

	/// <summary>
	/// Interface defining email notification delivery contracts across HealSync workflows.
	/// </summary>
	public interface IEmailService
	{
		/// <summary>
		/// Event raised when an email delivery is dispatched.
		/// </summary>
		event EmailSendEventHandler EmailSendEvent;

		/// <summary>
		/// Asynchronously dispatches a generic email message.
		/// </summary>
		/// <param name="toEmail">Target recipient email address.</param>
		/// <param name="subject">Email subject line.</param>
		/// <param name="body">HTML body content.</param>
		Task SendEmailAsync(string toEmail, string subject, string body);

		/// <summary>
		/// Asynchronously sends an email verification OTP code to a newly registered user.
		/// </summary>
		/// <param name="toEmail">Target recipient email address.</param>
		/// <param name="firstName">User's first name.</param>
		/// <param name="lastName">User's last name.</param>
		/// <param name="otp">One-Time Password code string.</param>
		/// <param name="expiryMinutes">Expiration duration in minutes (default: 15).</param>
		Task SendOtpVerificationEmailAsync(string toEmail, string firstName, string lastName, string otp, int expiryMinutes = 15);

		/// <summary>
		/// Asynchronously sends a password reset OTP verification code.
		/// </summary>
		/// <param name="toEmail">Target recipient email address.</param>
		/// <param name="firstName">User's first name.</param>
		/// <param name="lastName">User's last name.</param>
		/// <param name="otp">One-Time Password code string.</param>
		Task SendPasswordResetEmailAsync(string toEmail, string firstName, string lastName, string otp);

		/// <summary>
		/// Asynchronously sends an appointment confirmation notification email to a patient.
		/// </summary>
		/// <param name="toEmail">Target recipient email address.</param>
		/// <param name="firstName">Patient's first name.</param>
		/// <param name="lastName">Patient's last name.</param>
		/// <param name="appointmentDetails">HTML or plain text appointment schedule summary.</param>
		Task SendAppointmentConfirmationAsync(string toEmail, string firstName, string lastName, string appointmentDetails);

		/// <summary>
		/// Asynchronously sends an appointment cancellation alert email to a patient.
		/// </summary>
		/// <param name="toEmail">Target recipient email address.</param>
		/// <param name="firstName">Patient's first name.</param>
		/// <param name="lastName">Patient's last name.</param>
		/// <param name="appointmentDetails">Appointment schedule summary.</param>
		/// <param name="reason">Explanatory cancellation reason string.</param>
		Task SendAppointmentCancellationAsync(string toEmail, string firstName, string lastName, string appointmentDetails, string reason);

		/// <summary>
		/// Asynchronously sends a doctor account verification approval or rejection status email.
		/// </summary>
		/// <param name="toEmail">Target doctor's email address.</param>
		/// <param name="firstName">Doctor's first name.</param>
		/// <param name="lastName">Doctor's last name.</param>
		/// <param name="doctorName">Doctor's full display name.</param>
		/// <param name="isApproved">Indicates whether the account was approved (true) or rejected (false).</param>
		/// <param name="rejectionReason">Optional explanation text if rejected.</param>
		Task SendDoctorVerificationEmailAsync(string toEmail, string firstName, string lastName, string doctorName, bool isApproved, string? rejectionReason = null);

		/// <summary>
		/// Asynchronously sends a clinic branch verification approval or rejection status email to the managing clinic administrator.
		/// </summary>
		/// <param name="toEmail">Target administrator's email address.</param>
		/// <param name="adminFirstName">Administrator's first name.</param>
		/// <param name="adminLastName">Administrator's last name.</param>
		/// <param name="clinicName">Name of the clinic branch.</param>
		/// <param name="isApproved">Indicates whether the clinic branch was approved (true) or rejected (false).</param>
		/// <param name="rejectionReason">Optional explanation text if rejected.</param>
		Task SendClinicVerificationEmailAsync(string toEmail, string adminFirstName, string adminLastName, string clinicName, bool isApproved, string? rejectionReason = null);

		/// <summary>
		/// Asynchronously sends an onboarding application acknowledgment email to a doctor upon initial registration.
		/// </summary>
		/// <param name="toEmail">Target doctor's email address.</param>
		/// <param name="firstName">Doctor's first name.</param>
		/// <param name="lastName">Doctor's last name.</param>
		Task SendDoctorOnboardingReceivedEmailAsync(string toEmail, string firstName, string lastName);
	}

	#endregion

	/// <summary>
	/// Service implementation for sending system emails via SMTP.
	/// Supports HTML template formatting, background Hangfire queue processing, and multi-key configuration fallbacks.
	/// </summary>
	public class EmailService : IEmailService
	{
		private readonly IConfiguration _configuration;
		private readonly ILogger<EmailService> _logger;

		/// <summary>
		/// Event raised when an email is dispatched.
		/// </summary>
		public event EmailSendEventHandler? EmailSendEvent;

		/// <summary>
		/// Initializes a new instance of the <see cref="EmailService"/> class.
		/// </summary>
		/// <param name="configuration">System application configuration provider.</param>
		/// <param name="logger">Structured logger instance.</param>
		public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
		{
			_configuration = configuration;
			_logger = logger;
		}

		/// <summary>
		/// Directly executes SMTP delivery so callers and background workers capture exact exception tracebacks.
		/// </summary>
		/// <param name="toEmail">Target recipient email address.</param>
		/// <param name="subject">Email subject line.</param>
		/// <param name="body">HTML body content.</param>
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

			await SendEmailInternalAsync(toEmail, subject, body);
		}

		/// <summary>
		/// Actual SMTP delivery implementation with flexible key resolution across MailSettings, SmtpSettings, and environment variables.
		/// </summary>
		/// <param name="toEmail">Target recipient email address.</param>
		/// <param name="subject">Email subject line.</param>
		/// <param name="body">HTML body content.</param>
		private async Task SendEmailInternalAsync(
			string toEmail,
			string subject,
			string body)
		{
			// Flexible key resolution supporting MailSettings, SmtpSettings, and environment variable overrides
			var host = _configuration["MailSettings:Host"] 
				?? _configuration["SmtpSettings:Server"] 
				?? _configuration["SMTP_HOST"] 
				?? "smtp.gmail.com";

			var portStr = _configuration["MailSettings:Port"] 
				?? _configuration["SmtpSettings:Port"] 
				?? _configuration["SMTP_PORT"] 
				?? "587";
			int port = int.TryParse(portStr, out int p) ? p : 587;

			var senderEmail = _configuration["MailSettings:Mail"] 
				?? _configuration["MailSettings:Username"] 
				?? _configuration["SmtpSettings:SenderEmail"] 
				?? _configuration["SmtpSettings:Username"] 
				?? _configuration["SMTP_EMAIL"] 
				?? _configuration["SMTP_USERNAME"];

			var senderName = _configuration["MailSettings:DisplayName"] 
				?? _configuration["SmtpSettings:SenderName"] 
				?? "HealSync Medical Network";

			var password = _configuration["MailSettings:Password"] 
				?? _configuration["SmtpSettings:Password"] 
				?? _configuration["SMTP_PASSWORD"];

			if (!string.IsNullOrWhiteSpace(password))
			{
				password = password.Replace(" ", "").Trim();
			}

			if (string.IsNullOrWhiteSpace(senderEmail) || string.IsNullOrWhiteSpace(password))
			{
				var errMsg = "[EmailService] SMTP Sender Email or Password is not configured in application settings or environment variables.";
				_logger.LogError(errMsg);
				throw new InvalidOperationException(errMsg);
			}

			_logger.LogInformation("[EmailService] Sending email to {ToEmail} via SMTP Server {Host}:{Port} with Sender {SenderEmail}", toEmail, host, port, senderEmail);

			using var client = new SmtpClient(host, port)
			{
				EnableSsl = true,
				Credentials = new NetworkCredential(senderEmail, password),
				Timeout = 15000 // 15s timeout
			};

			var mail = new MailMessage
			{
				From = new MailAddress(senderEmail, senderName),
				Subject = subject,
				Body = body,
				IsBodyHtml = true
			};

			mail.To.Add(toEmail);

			try
			{
				await client.SendMailAsync(mail);
				_logger.LogInformation("[EmailService] Email successfully dispatched to {ToEmail}", toEmail);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "[EmailService] Failed to send email to {ToEmail} via {Host}:{Port}", toEmail, host, port);
				throw; // Throw exception so Hangfire records job failure if SMTP fails
			}
		}

		/// <summary>
		/// Creates the common HTML template wrapped around notification messages.
		/// </summary>
		/// <param name="title">Main section heading title.</param>
		/// <param name="titleColor">HEX color code string for the title header.</param>
		/// <param name="content">HTML inner content payload.</param>
		/// <param name="portalUrl">Target portal URL for the call-to-action button.</param>
		/// <param name="buttonText">Call-to-action button label text.</param>
		/// <returns>Formatted full HTML string.</returns>
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
		/// Asynchronously sends an email verification OTP code to a newly registered user.
		/// </summary>
		/// <param name="toEmail">Target recipient email address.</param>
		/// <param name="firstName">User's first name.</param>
		/// <param name="lastName">User's last name.</param>
		/// <param name="otp">One-Time Password code string.</param>
		/// <param name="expiryMinutes">Expiration duration in minutes (default: 15).</param>
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
<p>Thank you for signing up with <strong>HealSync</strong>. Please use the following One-Time Password (OTP) to verify your email address.</p>
<div style='text-align:center;margin:35px 0;'>
<span style='font-size:32px;font-weight:bold;letter-spacing:8px;padding:12px 26px;background:#f3f4f6;border:1px solid #d1d5db;border-radius:8px;'>
{otp}
</span>
</div>
<p>This OTP is valid for <strong>{expiryMinutes} minutes</strong>.</p>
<p style='color:#ef4444'>Do not share this OTP with anyone.</p>";

			var body = BuildTemplate("Email Verification", "#06b6d4", content);
			await SendEmailAsync(toEmail, subject, body);
		}

		/// <summary>
		/// Asynchronously sends a password reset OTP verification code.
		/// </summary>
		/// <param name="toEmail">Target recipient email address.</param>
		/// <param name="firstName">User's first name.</param>
		/// <param name="lastName">User's last name.</param>
		/// <param name="otp">One-Time Password code string.</param>
		public async Task SendPasswordResetEmailAsync(
			string toEmail,
			string firstName,
			string lastName,
			string otp)
		{
			var subject = "HealSync - Password Reset";
			var content = $@"
<p>Hello,</p>
<p>We received a request to reset your password. Please use the following OTP.</p>
<div style='text-align:center;margin:35px 0;'>
<span style='font-size:32px;font-weight:bold;letter-spacing:8px;padding:12px 26px;background:#fef2f2;border:1px solid #fecaca;border-radius:8px;color:#dc2626;'>
{otp}
</span>
</div>
<p>This OTP will expire in <strong>15 minutes</strong>.</p>
<p>If you did not request a password reset, you may safely ignore this email.</p>";

			var body = BuildTemplate("Password Reset", "#ef4444", content);
			await SendEmailAsync(toEmail, subject, body);
		}

		/// <summary>
		/// Asynchronously sends an appointment confirmation notification email to a patient.
		/// </summary>
		/// <param name="toEmail">Target recipient email address.</param>
		/// <param name="firstName">Patient's first name.</param>
		/// <param name="lastName">Patient's last name.</param>
		/// <param name="appointmentDetails">HTML or plain text appointment schedule summary.</param>
		public async Task SendAppointmentConfirmationAsync(
			string toEmail,
			string firstName,
			string lastName,
			string appointmentDetails)
		{
			var subject = "HealSync - Appointment Confirmed";
			var content = $@"
<p>Hello,</p>
<p>Your appointment has been successfully confirmed.</p>
<div style='background:#ecfdf5;padding:18px;border-radius:8px;border:1px solid #bbf7d0;margin:20px 0;'>
{appointmentDetails}
</div>
<p>Please arrive at least <strong>15 minutes</strong> before your appointment.</p>
<p>Thank you for choosing HealSync.</p>";

			var body = BuildTemplate("Appointment Confirmed", "#16a34a", content);
			await SendEmailAsync(toEmail, subject, body);
		}

		/// <summary>
		/// Asynchronously sends an appointment cancellation alert email to a patient.
		/// </summary>
		/// <param name="toEmail">Target recipient email address.</param>
		/// <param name="firstName">Patient's first name.</param>
		/// <param name="lastName">Patient's last name.</param>
		/// <param name="appointmentDetails">Appointment schedule summary.</param>
		/// <param name="reason">Explanatory cancellation reason string.</param>
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
<p>We regret to inform you that your appointment has been cancelled.</p>
<div style='background:#fef2f2;padding:18px;border-radius:8px;border:1px solid #fecaca;margin:20px 0;'>
{appointmentDetails}
</div>
<p><b>Reason:</b><br/><br/>{reason}</p>
<p>Please log in to HealSync to book another appointment.</p>";

			var body = BuildTemplate("Appointment Cancelled", "#dc2626", content);
			await SendEmailAsync(toEmail, subject, body);
		}

		/// <summary>
		/// Asynchronously sends a doctor account verification approval or rejection status email.
		/// </summary>
		/// <param name="toEmail">Target doctor's email address.</param>
		/// <param name="firstName">Doctor's first name.</param>
		/// <param name="lastName">Doctor's last name.</param>
		/// <param name="doctorName">Doctor's full display name.</param>
		/// <param name="isApproved">Indicates whether the account was approved (true) or rejected (false).</param>
		/// <param name="rejectionReason">Optional explanation text if rejected.</param>
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
<p>Dear Dr. <b>{doctorName}</b>,</p>
<p>Congratulations! Your profile has been successfully verified by the HealSync Administration Team.</p>
<p>You can now log in and begin accepting appointments from patients.</p>
<p>Welcome to HealSync!</p>";
			}
			else
			{
				subject = "HealSync - Doctor Profile Rejected";
				title = "Verification Failed";
				color = "#f59e0b";
				content = $@"
<p>Dear Dr. <b>{doctorName}</b>,</p>
<p>Unfortunately, we could not approve your profile verification.</p>
<p><b>Reason:</b></p>
<div style='background:#fffbeb;padding:15px;border:1px solid #fde68a;border-radius:8px;'>
{rejectionReason}
</div>
<p>You may update your information and submit your profile again.</p>";
			}

			var body = BuildTemplate(title, color, content, "https://healsync-medical.web.app/doctor/dashboard", "Open Doctor Portal &rarr;");
			await SendEmailAsync(toEmail, subject, body);
		}

		/// <summary>
		/// Asynchronously sends a clinic branch verification approval or rejection status email to the managing clinic administrator.
		/// </summary>
		/// <param name="toEmail">Target administrator's email address.</param>
		/// <param name="adminFirstName">Administrator's first name.</param>
		/// <param name="adminLastName">Administrator's last name.</param>
		/// <param name="clinicName">Name of the clinic branch.</param>
		/// <param name="isApproved">Indicates whether the clinic branch was approved (true) or rejected (false).</param>
		/// <param name="rejectionReason">Optional explanation text if rejected.</param>
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
<p>Congratulations! Your clinic <b>{clinicName}</b> has been approved successfully.</p>
<p>Patients can now book appointments for this clinic.</p>";
			}
			else
			{
				subject = "HealSync - Clinic Rejected";
				title = "Clinic Rejected";
				color = "#f59e0b";
				content = $@"
<p>Unfortunately, your clinic <b>{clinicName}</b> could not be approved.</p>
<p><b>Reason:</b></p>
<div style='background:#fffbeb;padding:15px;border-radius:8px;border:1px solid #fde68a;'>
{rejectionReason}
</div>
<p>Please correct the issues and submit the clinic again.</p>";
			}

			var body = BuildTemplate(title, color, content, "https://healsync-medical.web.app/admin/dashboard", "Access Admin Console &rarr;");
			await SendEmailAsync(toEmail, subject, body);
		}

		/// <summary>
		/// Asynchronously sends an onboarding application acknowledgment email to a doctor upon initial registration.
		/// </summary>
		/// <param name="toEmail">Target doctor's email address.</param>
		/// <param name="firstName">Doctor's first name.</param>
		/// <param name="lastName">Doctor's last name.</param>
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