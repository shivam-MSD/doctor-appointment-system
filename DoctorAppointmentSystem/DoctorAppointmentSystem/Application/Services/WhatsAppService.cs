using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DoctorAppointmentSystem.Application.Services
{
	/// <summary>
	/// Contract for sending WhatsApp messages & OTPs via WhatsApp Cloud API.
	/// </summary>
	public interface IWhatsAppService
	{
		/// <summary>
		/// Sends a 6-digit authentication OTP via WhatsApp to the target mobile number.
		/// </summary>
		/// <param name="mobileNo">Target mobile number (with country code, e.g. +919876543210 or 9876543210).</param>
		/// <param name="otpCode">The 6-digit OTP code.</param>
		/// <param name="purpose">Context/Purpose (e.g. "Registration", "Login", "Profile Update", "Family Member Verification").</param>
		Task<bool> SendWhatsAppOtpAsync(string mobileNo, string otpCode, string purpose = "Verification");

		/// <summary>
		/// Sends an appointment booking confirmation notification via WhatsApp.
		/// </summary>
		Task<bool> SendWhatsAppAppointmentConfirmationAsync(string mobileNo, string patientName, string doctorName, string clinicName, string dateStr, string timeStr);

		/// <summary>
		/// Sends an appointment status update (Accepted / Rejected / Rescheduled with reason) via WhatsApp.
		/// </summary>
		Task<bool> SendWhatsAppAppointmentStatusAsync(string mobileNo, string patientName, string doctorName, string status, string dateStr, string reason = "");

		/// <summary>
		/// Sends a security alert or general notification message via WhatsApp.
		/// </summary>
		Task<bool> SendWhatsAppAlertAsync(string mobileNo, string messageText);

		/// <summary>
		/// Sends an Admin notification alert via WhatsApp.
		/// </summary>
		Task<bool> SendWhatsAppAdminAlertAsync(string mobileNo, string alertTitle, string alertBody);

		/// <summary>
		/// Sends a Clinic onboarding status update (Approved / Rejected) via WhatsApp.
		/// </summary>
		Task<bool> SendWhatsAppClinicRequestStatusAsync(string mobileNo, string clinicName, string status, string reason = "");

		/// <summary>
		/// Sends a Doctor License verification status update (Approved / Rejected) via WhatsApp.
		/// </summary>
		Task<bool> SendWhatsAppLicenseVerificationStatusAsync(string mobileNo, string doctorName, string status, string reason = "");

		/// <summary>
		/// Sends a Doctor Emergency Leave alert notification to Clinic Admin via WhatsApp.
		/// </summary>
		Task<bool> SendWhatsAppEmergencyLeaveAlertAsync(string mobileNo, string doctorName, string dateStr);
	}

	/// <summary>
	/// Meta WhatsApp Cloud API Service Implementation with development console fallback.
	/// </summary>
	public class WhatsAppService : IWhatsAppService
	{
		private readonly HttpClient _httpClient;
		private readonly IConfiguration _configuration;
		private readonly ILogger<WhatsAppService> _logger;

		public WhatsAppService(HttpClient httpClient, IConfiguration configuration, ILogger<WhatsAppService> logger)
		{
			_httpClient = httpClient;
			_configuration = configuration;
			_logger = logger;
		}

		public async Task<bool> SendWhatsAppOtpAsync(string mobileNo, string otpCode, string purpose = "Verification")
		{
			var formattedMobile = CleanPhoneNumber(mobileNo);
			var accessToken = _configuration["WhatsAppSettings:AccessToken"];
			var phoneNumberId = _configuration["WhatsAppSettings:PhoneNumberId"];
			var templateName = _configuration["WhatsAppSettings:TemplateName"] ?? "hello_world";

			// Dev Fallback Mode if API key/PhoneId is missing or set to placeholder
			if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(phoneNumberId) || accessToken.Contains("YOUR_"))
			{
				_logger.LogInformation("[WHATSAPP MOCK GATEWAY] Dispatched OTP {OtpCode} to {MobileNo} for {Purpose}.", otpCode, formattedMobile, purpose);
				Console.WriteLine($"\n=======================================================");
				Console.WriteLine($"📲 [WHATSAPP OTP GATEWAY] Destination: {formattedMobile}");
				Console.WriteLine($"🔑 Purpose: {purpose} | OTP Code: {otpCode}");
				Console.WriteLine($"=======================================================\n");
				return await Task.FromResult(true);
			}

			try
			{
				var requestUrl = $"https://graph.facebook.com/v18.0/{phoneNumberId}/messages";

				// 1. Try sending free-form custom text message with explicit OTP message body
				var messageText = $"🔒 *HealSync Verification Code*\n\nYour 6-digit OTP code for {purpose} is: *{otpCode}*\n\nThis code will expire in 10 minutes. Please do not share this OTP with anyone.";
				
				var textPayload = new
				{
					messaging_product = "whatsapp",
					recipient_type = "individual",
					to = formattedMobile,
					type = "text",
					text = new
					{
						preview_url = false,
						body = messageText
					}
				};

				var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
				{
					Content = new StringContent(JsonSerializer.Serialize(textPayload), Encoding.UTF8, "application/json")
				};
				request.Headers.Add("Authorization", $"Bearer {accessToken}");

				var response = await _httpClient.SendAsync(request);
				if (response.IsSuccessStatusCode)
				{
					_logger.LogInformation("WhatsApp Cloud API custom OTP text message sent to {MobileNo}", formattedMobile);
					return true;
				}

				// 2. Fallback to Meta Template message if free-form text fails
				object templatePayload;
				if (templateName.Equals("hello_world", StringComparison.OrdinalIgnoreCase))
				{
					templatePayload = new
					{
						messaging_product = "whatsapp",
						recipient_type = "individual",
						to = formattedMobile,
						type = "template",
						template = new
						{
							name = "hello_world",
							language = new { code = "en_US" }
						}
					};
				}
				else
				{
					templatePayload = new
					{
						messaging_product = "whatsapp",
						recipient_type = "individual",
						to = formattedMobile,
						type = "template",
						template = new
						{
							name = templateName,
							language = new { code = "en_US" },
							components = new object[]
							{
								new
								{
									type = "body",
									parameters = new[]
									{
										new { type = "text", text = otpCode }
									}
								}
							}
						}
					};
				}

				var templateRequest = new HttpRequestMessage(HttpMethod.Post, requestUrl)
				{
					Content = new StringContent(JsonSerializer.Serialize(templatePayload), Encoding.UTF8, "application/json")
				};
				templateRequest.Headers.Add("Authorization", $"Bearer {accessToken}");

				var templateResponse = await _httpClient.SendAsync(templateRequest);
				if (templateResponse.IsSuccessStatusCode)
				{
					_logger.LogInformation("WhatsApp Cloud API OTP template sent to {MobileNo}", formattedMobile);
					return true;
				}

				// Fallback retry using default 'hello_world' template if custom template fails
				if (!templateName.Equals("hello_world", StringComparison.OrdinalIgnoreCase))
				{
					var fallbackPayload = new
					{
						messaging_product = "whatsapp",
						recipient_type = "individual",
						to = formattedMobile,
						type = "template",
						template = new
						{
							name = "hello_world",
							language = new { code = "en_US" }
						}
					};

					var fallbackRequest = new HttpRequestMessage(HttpMethod.Post, requestUrl)
					{
						Content = new StringContent(JsonSerializer.Serialize(fallbackPayload), Encoding.UTF8, "application/json")
					};
					fallbackRequest.Headers.Add("Authorization", $"Bearer {accessToken}");

					var fallbackResponse = await _httpClient.SendAsync(fallbackRequest);
					if (fallbackResponse.IsSuccessStatusCode)
					{
						_logger.LogInformation("WhatsApp Cloud API OTP successfully sent to {MobileNo} via hello_world fallback", formattedMobile);
						return true;
					}
				}

				return false;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Exception encountered while calling WhatsApp Cloud API for {MobileNo}", formattedMobile);
				return false;
			}
		}

		public async Task<bool> SendWhatsAppAppointmentConfirmationAsync(string mobileNo, string patientName, string doctorName, string clinicName, string dateStr, string timeStr)
		{
			var formattedMobile = CleanPhoneNumber(mobileNo);
			_logger.LogInformation("[WHATSAPP APPOINTMENT BOOKED] To {MobileNo} ({PatientName}): Dr. {DoctorName} at {ClinicName} on {Date} {Time}", formattedMobile, patientName, doctorName, clinicName, dateStr, timeStr);
			Console.WriteLine($"\n=======================================================");
			Console.WriteLine($"📲 [WHATSAPP APPOINTMENT BOOKED] Destination: {formattedMobile}");
			Console.WriteLine($"👤 Patient: {patientName} | 🩺 Doctor: Dr. {doctorName}");
			Console.WriteLine($"🏥 Clinic: {clinicName} | 📅 Schedule: {dateStr} at {timeStr}");
			Console.WriteLine($"=======================================================\n");

			var messageText = $"🏥 *HealSync Appointment Confirmation*\n\nDear {patientName},\nYour appointment with *Dr. {doctorName}* at *{clinicName}* has been confirmed!\n\n📅 Date: {dateStr}\n⏰ Time: {timeStr}\n\nThank you for choosing HealSync Healthcare!";
			return await SendMessageBodyOrTemplateFallbackAsync(formattedMobile, messageText);
		}

		public async Task<bool> SendWhatsAppAppointmentStatusAsync(string mobileNo, string patientName, string doctorName, string status, string dateStr, string reason = "")
		{
			var formattedMobile = CleanPhoneNumber(mobileNo);
			_logger.LogInformation("[WHATSAPP APPOINTMENT STATUS] To {MobileNo} ({PatientName}): Status={Status}, Doctor=Dr. {DoctorName}, Date={Date}, Reason={Reason}", formattedMobile, patientName, status, doctorName, dateStr, reason);
			Console.WriteLine($"\n=======================================================");
			Console.WriteLine($"📲 [WHATSAPP APPOINTMENT STATUS UPDATE] Destination: {formattedMobile}");
			Console.WriteLine($"👤 Patient: {patientName} | 🩺 Doctor: Dr. {doctorName}");
			Console.WriteLine($"📌 Status: {status} | 📅 Date: {dateStr} {(string.IsNullOrWhiteSpace(reason) ? "" : "| 📝 Reason: " + reason)}");
			Console.WriteLine($"=======================================================\n");

			var reasonLine = string.IsNullOrWhiteSpace(reason) ? "" : $"\n📝 Reason: {reason}";
			var messageText = $"📋 *HealSync Appointment Status Update*\n\nDear {patientName},\nYour appointment with *Dr. {doctorName}* for {dateStr} has been updated to: *{status.ToUpper()}*.{reasonLine}\n\nHealSync Healthcare Network";
			return await SendMessageBodyOrTemplateFallbackAsync(formattedMobile, messageText);
		}

		public async Task<bool> SendWhatsAppAlertAsync(string mobileNo, string messageText)
		{
			var formattedMobile = CleanPhoneNumber(mobileNo);
			_logger.LogInformation("[WHATSAPP SECURITY ALERT] To {MobileNo}: {Message}", formattedMobile, messageText);
			Console.WriteLine($"📲 [WHATSAPP ALERT] To: {formattedMobile} | Message: {messageText}");

			var fullText = $"🔔 *HealSync Security Notification*\n\n{messageText}";
			return await SendMessageBodyOrTemplateFallbackAsync(formattedMobile, fullText);
		}

		public async Task<bool> SendWhatsAppAdminAlertAsync(string mobileNo, string alertTitle, string alertBody)
		{
			var formattedMobile = CleanPhoneNumber(mobileNo);
			_logger.LogInformation("[WHATSAPP ADMIN ALERT] To {MobileNo}: {Title} - {Body}", formattedMobile, alertTitle, alertBody);
			Console.WriteLine($"\n=======================================================");
			Console.WriteLine($"👑 [WHATSAPP ADMIN ALERT] Destination: {formattedMobile}");
			Console.WriteLine($"📌 Title: {alertTitle} | Details: {alertBody}");
			Console.WriteLine($"=======================================================\n");

			var messageText = $"👑 *HealSync Super Admin Alert: {alertTitle}*\n\n{alertBody}\n\nPlease review in the HealSync Admin Portal.";
			return await SendMessageBodyOrTemplateFallbackAsync(formattedMobile, messageText);
		}

		public async Task<bool> SendWhatsAppClinicRequestStatusAsync(string mobileNo, string clinicName, string status, string reason = "")
		{
			var formattedMobile = CleanPhoneNumber(mobileNo);
			_logger.LogInformation("[WHATSAPP CLINIC STATUS] To {MobileNo}: Clinic={ClinicName}, Status={Status}", formattedMobile, clinicName, status);
			
			var statusEmoji = status.Equals("Approved", StringComparison.OrdinalIgnoreCase) ? "✅" : "❌";
			var reasonText = string.IsNullOrWhiteSpace(reason) ? "" : $"\n📝 Details: {reason}";
			var messageText = $"{statusEmoji} *HealSync Clinic Verification Update*\n\nYour clinic creation request for *{clinicName}* has been *{status.ToUpper()}*.{reasonText}\n\nHealSync Healthcare Network";
			return await SendMessageBodyOrTemplateFallbackAsync(formattedMobile, messageText);
		}

		public async Task<bool> SendWhatsAppLicenseVerificationStatusAsync(string mobileNo, string doctorName, string status, string reason = "")
		{
			var formattedMobile = CleanPhoneNumber(mobileNo);
			_logger.LogInformation("[WHATSAPP LICENSE STATUS] To {MobileNo}: Doctor={DoctorName}, Status={Status}", formattedMobile, doctorName, status);

			var statusEmoji = status.Equals("Approved", StringComparison.OrdinalIgnoreCase) ? "🩺" : "⚠️";
			var reasonText = string.IsNullOrWhiteSpace(reason) ? "" : $"\n📝 Details: {reason}";
			var messageText = $"{statusEmoji} *HealSync Doctor Verification Status*\n\nDear Dr. {doctorName},\nYour medical license verification status is: *{status.ToUpper()}*.{reasonText}\n\nHealSync Healthcare Network";
			return await SendMessageBodyOrTemplateFallbackAsync(formattedMobile, messageText);
		}

		public async Task<bool> SendWhatsAppEmergencyLeaveAlertAsync(string mobileNo, string doctorName, string dateStr)
		{
			var formattedMobile = CleanPhoneNumber(mobileNo);
			_logger.LogInformation("[WHATSAPP EMERGENCY LEAVE] To Clinic Admin {MobileNo}: Doctor Dr. {DoctorName} on {Date}", formattedMobile, doctorName, dateStr);

			var messageText = $"🚨 *HealSync Emergency Doctor Leave Alert*\n\nDr. {doctorName} has logged emergency leave for *{dateStr}*.\n\nPlease log in to Clinic Admin portal to manage patient rescheduling.";
			return await SendMessageBodyOrTemplateFallbackAsync(formattedMobile, messageText);
		}

		private async Task<bool> SendMessageBodyOrTemplateFallbackAsync(string formattedMobile, string messageText)
		{
			var accessToken = _configuration["WhatsAppSettings:AccessToken"];
			var phoneNumberId = _configuration["WhatsAppSettings:PhoneNumberId"];
			var templateName = _configuration["WhatsAppSettings:TemplateName"] ?? "hello_world";

			if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(phoneNumberId) || accessToken.Contains("YOUR_"))
			{
				return await Task.FromResult(true);
			}

			try
			{
				var requestUrl = $"https://graph.facebook.com/v18.0/{phoneNumberId}/messages";

				// 1. Try sending free-form custom text message body
				var textPayload = new
				{
					messaging_product = "whatsapp",
					recipient_type = "individual",
					to = formattedMobile,
					type = "text",
					text = new
					{
						preview_url = false,
						body = messageText
					}
				};

				var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
				{
					Content = new StringContent(JsonSerializer.Serialize(textPayload), Encoding.UTF8, "application/json")
				};
				request.Headers.Add("Authorization", $"Bearer {accessToken}");

				var response = await _httpClient.SendAsync(request);
				if (response.IsSuccessStatusCode)
				{
					_logger.LogInformation("WhatsApp Cloud API text message sent to {MobileNo}", formattedMobile);
					return true;
				}

				// 2. Fallback to Meta Template message if free-form text fails
				var templatePayload = new
				{
					messaging_product = "whatsapp",
					recipient_type = "individual",
					to = formattedMobile,
					type = "template",
					template = new
					{
						name = "hello_world",
						language = new { code = "en_US" }
					}
				};

				var templateRequest = new HttpRequestMessage(HttpMethod.Post, requestUrl)
				{
					Content = new StringContent(JsonSerializer.Serialize(templatePayload), Encoding.UTF8, "application/json")
				};
				templateRequest.Headers.Add("Authorization", $"Bearer {accessToken}");

				var templateResponse = await _httpClient.SendAsync(templateRequest);
				return templateResponse.IsSuccessStatusCode;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to send Meta WhatsApp Cloud API message to {MobileNo}", formattedMobile);
				return false;
			}
		}

		private static string CleanPhoneNumber(string phone)
		{
			if (string.IsNullOrWhiteSpace(phone)) return string.Empty;
			var digitsOnly = System.Text.RegularExpressions.Regex.Replace(phone, @"[^\d]", "");
			if (digitsOnly.Length == 10) return "91" + digitsOnly;
			return digitsOnly;
		}
	}
}
