using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DoctorAppointmentSystem.Domain.Entities;
using DoctorAppointmentSystem.Persistent.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DoctorAppointmentSystem.Application.Services
{
	public class WebPushService : IWebPushService
	{
		private readonly ApplicationDbContext _dbContext;
		private readonly ILogger<WebPushService> _logger;
		private static readonly HttpClient _httpClient = new HttpClient();

		public WebPushService(ApplicationDbContext dbContext, ILogger<WebPushService> logger)
		{
			_dbContext = dbContext;
			_logger = logger;
		}

		public async Task SaveSubscriptionAsync(string userId, string endpoint, string p256dh, string auth)
		{
			if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(endpoint))
				return;

			var existing = await _dbContext.UserPushSubscriptions
				.FirstOrDefaultAsync(s => s.UserId == userId && s.Endpoint == endpoint);

			if (existing == null)
			{
				var sub = new UserPushSubscription
				{
					UserId = userId,
					Endpoint = endpoint,
					P256dh = p256dh ?? string.Empty,
					Auth = auth ?? string.Empty,
					CreatedAt = DateTime.UtcNow
				};
				_dbContext.UserPushSubscriptions.Add(sub);
			}
			else
			{
				existing.P256dh = p256dh ?? string.Empty;
				existing.Auth = auth ?? string.Empty;
			}

			await _dbContext.SaveChangesAsync();
		}

		public async Task SendPushNotificationAsync(string userId, string title, string message, string portalUrl = "https://healsync-medical.web.app/patient/dashboard")
		{
			if (string.IsNullOrWhiteSpace(userId)) return;

			var subs = await _dbContext.UserPushSubscriptions
				.Where(s => s.UserId == userId)
				.ToListAsync();

			if (!subs.Any()) return;

			var payload = new
			{
				notification = new
				{
					title = title,
					body = message,
					icon = "/assets/icons/icon-192x192.png",
					badge = "/assets/icons/icon-72x72.png",
					data = new { url = portalUrl }
				}
			};

			string jsonPayload = JsonSerializer.Serialize(payload);

			foreach (var sub in subs)
			{
				try
				{
					using var request = new HttpRequestMessage(HttpMethod.Post, sub.Endpoint);
					request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

					var response = await _httpClient.SendAsync(request);
					if (response.StatusCode == System.Net.HttpStatusCode.Gone || response.StatusCode == System.Net.HttpStatusCode.NotFound)
					{
						_dbContext.UserPushSubscriptions.Remove(sub);
					}
				}
				catch (Exception ex)
				{
					_logger.LogWarning($"Failed to dispatch WebPush to endpoint {sub.Endpoint}: {ex.Message}");
				}
			}

			await _dbContext.SaveChangesAsync();
		}
	}
}
