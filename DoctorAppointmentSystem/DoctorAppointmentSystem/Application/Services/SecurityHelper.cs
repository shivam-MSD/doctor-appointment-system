using System;
using Microsoft.AspNetCore.Http;

namespace DoctorAppointmentSystem.Application.Services
{
	/// <summary>
	/// Helper utility class for generating security alert email templates and login tracking details.
	/// </summary>
	public static class SecurityHelper
	{
		/// <summary>
		/// Extracts the client IP address from the HTTP request context headers.
		/// </summary>
		public static string GetClientIpAddress(HttpContext? context)
		{
			if (context == null) return "127.0.0.1 (Localhost)";

			string? headerIp = context.Request.Headers["X-Forwarded-For"].ToString();
			if (!string.IsNullOrEmpty(headerIp))
			{
				return headerIp.Split(',')[0].Trim();
			}

			var remoteIp = context.Connection.RemoteIpAddress?.ToString();
			if (string.IsNullOrEmpty(remoteIp) || remoteIp == "::1")
			{
				return "127.0.0.1 (Localhost)";
			}

			return remoteIp;
		}

		/// <summary>
		/// Extracts device and browser information from the User-Agent header.
		/// </summary>
		public static string GetDeviceAndBrowserInfo(HttpContext? context)
		{
			if (context == null) return "Unknown Device / Browser";

			string userAgent = context.Request.Headers["User-Agent"].ToString();
			if (string.IsNullOrEmpty(userAgent)) return "Standard Web Client";

			string browser = "Browser";
			if (userAgent.Contains("Edg")) browser = "Microsoft Edge";
			else if (userAgent.Contains("Chrome")) browser = "Google Chrome";
			else if (userAgent.Contains("Firefox")) browser = "Mozilla Firefox";
			else if (userAgent.Contains("Safari")) browser = "Apple Safari";

			string os = "Device";
			if (userAgent.Contains("Windows")) os = "Windows PC";
			else if (userAgent.Contains("Mac OS")) os = "macOS";
			else if (userAgent.Contains("iPhone") || userAgent.Contains("iPad")) os = "iOS Device";
			else if (userAgent.Contains("Android")) os = "Android Mobile";

			return $"{browser} on {os}";
		}

		/// <summary>
		/// Builds a styled HTML email body for new login security notifications.
		/// </summary>
		public static string BuildLoginSecurityEmailHtml(string firstName, string role, string loginTimeStr, string ipAddress, string deviceInfo)
		{
			return $@"
<div style=""font-family: 'Inter', -apple-system, BlinkMacSystemFont, sans-serif; background-color: #f8fafc; padding: 20px; color: #0f172a;"">
  <div style=""max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.05); border: 1px solid #e2e8f0;"">
    
    <!-- Header -->
    <div style=""background: linear-gradient(135deg, #06b6d4 0%, #3b82f6 100%); padding: 24px; text-align: center; color: #ffffff;"">
      <h2 style=""margin: 0; font-size: 20px; font-weight: 700; letter-spacing: -0.5px;"">🔑 Security Alert: New Login</h2>
      <p style=""margin: 6px 0 0 0; font-size: 13px; opacity: 0.9;"">HealSync Account Security Notification</p>
    </div>

    <!-- Content -->
    <div style=""padding: 28px;"">
      <p style=""font-size: 14px; margin-top: 0;"">Hello {firstName},</p>
      <p style=""font-size: 14px; line-height: 1.5; color: #334155;"">
        We detected a new login to your HealSync account (<strong>{role} Portal</strong>). Here are the details of the login session:
      </p>

      <!-- Details Table -->
      <div style=""background-color: #f1f5f9; border-radius: 8px; padding: 16px; margin: 20px 0; border: 1px solid #e2e8f0;"">
        <table style=""width: 100%; border-collapse: collapse; font-size: 13px;"">
          <tr>
            <td style=""padding: 6px 0; color: #64748b; font-weight: 500; width: 130px;"">Login Time:</td>
            <td style=""padding: 6px 0; color: #0f172a; font-weight: 600;"">{loginTimeStr}</td>
          </tr>
          <tr>
            <td style=""padding: 6px 0; color: #64748b; font-weight: 500;"">Role Portal:</td>
            <td style=""padding: 6px 0; color: #06b6d4; font-weight: 700;"">{role}</td>
          </tr>
          <tr>
            <td style=""padding: 6px 0; color: #64748b; font-weight: 500;"">Device & Browser:</td>
            <td style=""padding: 6px 0; color: #0f172a; font-weight: 600;"">{deviceInfo}</td>
          </tr>
          <tr>
            <td style=""padding: 6px 0; color: #64748b; font-weight: 500;"">IP Address:</td>
            <td style=""padding: 6px 0; color: #0f172a; font-weight: 600; font-family: monospace;"">{ipAddress}</td>
          </tr>
        </table>
      </div>

      <p style=""font-size: 13px; color: #64748b; line-height: 1.5;"">If this was you, no action is required. If you did not log in at this time, please change your password immediately or contact our support team.</p>

      <div style=""text-align: center; margin-top: 24px;"">
        <a href=""https://healsync-medical.web.app/account/security?role={role}&action=review-login"" style=""background-color: #ef4444; color: #ffffff; text-decoration: none; padding: 10px 22px; border-radius: 6px; font-weight: 600; font-size: 13px; display: inline-block;"">
          Secure My Account &rarr;
        </a>
      </div>

      <p style=""font-size: 12px; color: #94a3b8; margin-top: 28px; border-top: 1px solid #e2e8f0; padding-top: 14px; text-align: center;"">HealSync Security Team • Automated System Notification</p>
    </div>
  </div>
</div>";
		}
	}
}
