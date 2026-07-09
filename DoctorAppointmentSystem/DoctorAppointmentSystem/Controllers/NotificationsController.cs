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

		public NotificationsController(INotificationService notificationService)
		{
			_notificationService = notificationService;
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
	}
}
