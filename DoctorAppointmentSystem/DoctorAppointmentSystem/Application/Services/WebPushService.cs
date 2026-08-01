using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DoctorAppointmentSystem.Domain.Entities;
using DoctorAppointmentSystem.Persistent.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WebPush;

namespace DoctorAppointmentSystem.Application.Services
{
	/// <summary>
	/// Service implementation for registering VAPID subscriptions and dispatching WebPush notifications to mobile devices when closed.
	/// </summary>
	public class WebPushService : IWebPushService
	{
		private readonly ApplicationDbContext _dbContext;
		private readonly IConfiguration _configuration;
		private readonly ILogger<WebPushService> _logger;

		private static VapidDetails? _cachedVapidDetails;
		private static string? _cachedPublicKey;

		public WebPushService(ApplicationDbContext dbContext, IConfiguration configuration, ILogger<WebPushService> logger)
		{
			_dbContext = dbContext;
			_configuration = configuration;
			_logger = logger;
		}

		private VapidDetails GetVapidDetails()
		{
			if (_cachedVapidDetails != null)
			{
				return _cachedVapidDetails;
			}

			var publicKey = _configuration["VapidKeys:PublicKey"];
			var privateKey = _configuration["VapidKeys:PrivateKey"];
			var subject = _configuration["VapidKeys:Subject"] ?? "mailto:shivapatel1102001@gmail.com";

			if (string.IsNullOrWhiteSpace(publicKey) || string.IsNullOrWhiteSpace(privateKey))
			{
				// Auto-generate mathematically valid matching ECDSA P-256 VAPID keypair
				var keys = VapidHelper.GenerateVapidKeys();
				publicKey = keys.PublicKey;
				privateKey = keys.PrivateKey;
				_logger.LogInformation("[WebPush] Generated new matching VAPID Keypair. PublicKey: {PublicKey}", publicKey);
			}

			_cachedVapidDetails = new VapidDetails(subject, publicKey, privateKey);
			return _cachedVapidDetails;
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

			var vapidDetails = GetVapidDetails();
			var webPushClient = new WebPushClient();

			var payload = new
			{
				notification = new
				{
					title = title,
					body = message,
					icon = "/assets/logo-192.png",
					badge = "/assets/logo-192.png",
					data = new { url = portalUrl }
				}
			};

			string jsonPayload = JsonSerializer.Serialize(payload);

			foreach (var sub in subs)
			{
				try
				{
					if (string.IsNullOrWhiteSpace(sub.P256dh) || string.IsNullOrWhiteSpace(sub.Auth))
						continue;

					var pushSubscription = new PushSubscription(sub.Endpoint, sub.P256dh, sub.Auth);
					await webPushClient.SendNotificationAsync(pushSubscription, jsonPayload, vapidDetails);
					_logger.LogInformation("[WebPush] Closed-app push notification dispatched to endpoint {Endpoint}", sub.Endpoint);
				}
				catch (WebPushException ex)
				{
					_logger.LogWarning("[WebPush] WebPushException ({StatusCode}) for endpoint {Endpoint}: {Message}", ex.StatusCode, sub.Endpoint, ex.Message);
					if (ex.StatusCode == System.Net.HttpStatusCode.Gone || ex.StatusCode == System.Net.HttpStatusCode.NotFound)
					{
						_dbContext.UserPushSubscriptions.Remove(sub);
					}
				}
				catch (Exception ex)
				{
					_logger.LogWarning(ex, "[WebPush] Failed to dispatch push notification to {Endpoint}", sub.Endpoint);
				}
			}

			await _dbContext.SaveChangesAsync();
		}
	}
}
