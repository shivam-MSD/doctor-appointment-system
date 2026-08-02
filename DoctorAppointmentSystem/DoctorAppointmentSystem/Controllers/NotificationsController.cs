using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DoctorAppointmentSystem.Application.Services;

namespace DoctorAppointmentSystem.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	[Authorize]
	public class NotificationsController : ControllerBase
	{
		private readonly INotificationService _notificationService;
		private readonly IWebPushService _webPushService;

		public NotificationsController(INotificationService notificationService, IWebPushService webPushService)
		{
			_notificationService = notificationService;
			_webPushService = webPushService;
		}

		[HttpGet("vapid-public-key")]
		[AllowAnonymous]
		public IActionResult GetVapidPublicKey()
		{
			var publicKey = _webPushService.GetVapidPublicKey();
			return Ok(new { PublicKey = publicKey });
		}

		[HttpGet]
		public async Task<IActionResult> GetNotifications([FromHeader(Name = "X-User-Id")] Guid userId)
		{
			if (userId == Guid.Empty)
			{
				return BadRequest("Missing required X-User-Id header representing the authenticated user.");
			}

			var notifications = await _notificationService.GetNotificationsForUserAsync(userId);
			return Ok(notifications);
		}

		[HttpPost("mark-read")]
		public async Task<IActionResult> MarkRead([FromHeader(Name = "X-User-Id")] Guid userId)
		{
			if (userId == Guid.Empty)
			{
				return BadRequest("Missing required X-User-Id header representing the authenticated user.");
			}

			await _notificationService.MarkAllAsReadAsync(userId);
			return Ok(new { Message = "All notifications marked as read." });
		}

		[HttpPost("subscribe-push")]
		public async Task<IActionResult> SubscribePush([FromHeader(Name = "X-User-Id")] Guid userId, [FromBody] PushSubscriptionDto dto)
		{
			if (userId == Guid.Empty || dto == null || string.IsNullOrWhiteSpace(dto.Endpoint))
			{
				return BadRequest("Invalid push subscription data.");
			}

			await _webPushService.SaveSubscriptionAsync(userId.ToString(), dto.Endpoint, dto.P256dh, dto.Auth);
			return Ok(new { Message = "Push subscription saved successfully." });
		}
	}

	public class PushSubscriptionDto
	{
		public string Endpoint { get; set; } = string.Empty;
		public string P256dh { get; set; } = string.Empty;
		public string Auth { get; set; } = string.Empty;
	}
}
