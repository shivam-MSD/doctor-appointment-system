using System;
using Microsoft.AspNetCore.Http;

namespace DoctorAppointmentSystem.Application.Services
{
	public static class SecurityHelper
	{
		public static string GetClientIpAddress(HttpContext? httpContext)
		{
			if (httpContext == null) return "Unknown IP";

			// Check common proxy headers (Render, Cloudflare, Nginx)
			string? ip = httpContext.Request.Headers["CF-Connecting-IP"].ToString();
			if (string.IsNullOrWhiteSpace(ip))
			{
				ip = httpContext.Request.Headers["X-Forwarded-For"].ToString();
				if (!string.IsNullOrWhiteSpace(ip) && ip.Contains(","))
				{
					ip = ip.Split(',')[0].Trim();
				}
			}

			if (string.IsNullOrWhiteSpace(ip))
			{
				ip = httpContext.Connection.RemoteIpAddress?.ToString();
			}

			return string.IsNullOrWhiteSpace(ip) || ip == "::1" || ip == "127.0.0.1" ? "127.0.0.1 (Localhost)" : ip;
		}

		public static string GetDeviceAndBrowserInfo(HttpContext? httpContext)
		{
			if (httpContext == null) return "Unknown Device";

			string userAgent = httpContext.Request.Headers["User-Agent"].ToString();
			if (string.IsNullOrWhiteSpace(userAgent)) return "Web Browser";

			string browser = "Web Browser";
			if (userAgent.Contains("Edg")) browser = "Microsoft Edge";
			else if (userAgent.Contains("Chrome")) browser = "Google Chrome";
			else if (userAgent.Contains("Firefox")) browser = "Mozilla Firefox";
			else if (userAgent.Contains("Safari") && !userAgent.Contains("Chrome")) browser = "Apple Safari";
			else if (userAgent.Contains("Opera") || userAgent.Contains("OPR")) browser = "Opera";

			string os = "Unknown OS";
			if (userAgent.Contains("Windows NT 10.0")) os = "Windows 10/11";
			else if (userAgent.Contains("Windows")) os = "Windows OS";
			else if (userAgent.Contains("Android")) os = "Android Mobile";
			else if (userAgent.Contains("iPhone") || userAgent.Contains("iPad")) os = "iOS (iPhone/iPad)";
			else if (userAgent.Contains("Mac OS X")) os = "macOS";
			else if (userAgent.Contains("Linux")) os = "Linux OS";

			return $"{browser} on {os}";
		}

		public static string BuildLoginSecurityEmailHtml(
			string userName,
			string role,
			string loginTimeStr,
			string ipAddress,
			string deviceInfo)
		{
			return $@"
<div style=""font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f8fafc; padding: 30px 15px;"">
  <div style=""max-width: 580px; margin: 0 auto; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 15px rgba(0,0,0,0.06); border: 1px solid #e2e8f0;"">
    <div style=""background: linear-gradient(135deg, #06b6d4 0%, #3b82f6 100%); padding: 24px; text-align: center;"">
      <h1 style=""color: #ffffff; margin: 0; font-size: 22px; font-weight: 700;"">🔐 Security Alert: New Login</h1>
      <p style=""color: #e0f2fe; margin: 6px 0 0 0; font-size: 13px;"">HealSync Account Security Notification</p>
    </div>
    
    <div style=""padding: 28px 24px;"">
      <p style=""font-size: 15px; color: #334155; margin-top: 0;"">Hello <strong>{userName}</strong>,</p>
      <p style=""font-size: 14px; color: #475569; line-height: 1.5;"">We noticed a new login to your HealSync account ({role} Portal). Here are the details of the session:</p>
      
      <div style=""background-color: #f1f5f9; border-left: 4px solid #06b6d4; padding: 16px; border-radius: 6px; margin: 20px 0;"">
        <table style=""width: 100%; border-collapse: collapse; font-size: 13px;"">
          <tr>
            <td style=""padding: 6px 0; color: #64748b; font-weight: 500; width: 35%;"">Login Time:</td>
            <td style=""padding: 6px 0; color: #0f172a; font-weight: 600;"">{loginTimeStr} (UTC)</td>
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
        <a href=""https://healsync-medical.web.app/forgot-password"" style=""background-color: #ef4444; color: #ffffff; text-decoration: none; padding: 10px 22px; border-radius: 6px; font-weight: 600; font-size: 13px; display: inline-block;"">
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
