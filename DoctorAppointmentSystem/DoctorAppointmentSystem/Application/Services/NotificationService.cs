using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using DoctorAppointmentSystem.Application.DTOs;
using DoctorAppointmentSystem.Domain.Entities;
using DoctorAppointmentSystem.Persistent.Context;
using DoctorAppointmentSystem.Application.Hubs;

namespace DoctorAppointmentSystem.Application.Services
{
	public class NotificationService : INotificationService
	{
		private readonly ApplicationDbContext _dbContext;
		private readonly IHubContext<NotificationHub> _hubContext;

		public NotificationService(ApplicationDbContext dbContext, IHubContext<NotificationHub> hubContext)
		{
			_dbContext = dbContext;
			_hubContext = hubContext;
		}

		public async Task<IEnumerable<NotificationDto>> GetNotificationsForUserAsync(Guid userId)
		{
			return await _dbContext.Notifications
				.Where(n => n.UserId == userId)
				.OrderByDescending(n => n.CreatedDate)
				.Select(n => new NotificationDto
				{
					NotificationId = n.NotificationId,
					Message = n.Message,
					IsRead = n.IsRead,
					CreatedDate = DateTime.SpecifyKind(n.CreatedDate, DateTimeKind.Utc)
				})
				.ToListAsync();
		}

		public async Task CreateNotificationAsync(Guid userId, string message)
		{
			var notification = new Notification
			{
				NotificationId = Guid.NewGuid(),
				UserId = userId,
				Message = message,
				IsRead = false,
				CreatedDate = DateTime.UtcNow
			};

			_dbContext.Notifications.Add(notification);
			await _dbContext.SaveChangesAsync();

			// Broadcast via SignalR group
			var dto = new NotificationDto
			{
				NotificationId = notification.NotificationId,
				Message = notification.Message,
				IsRead = notification.IsRead,
				CreatedDate = DateTime.SpecifyKind(notification.CreatedDate, DateTimeKind.Utc)
			};
			await _hubContext.Clients.Group(userId.ToString()).SendAsync("ReceiveNotification", dto);
		}

		public async Task CreateNotificationForRoleAsync(string roleName, string message)
		{
			if (!Enum.TryParse<ERole>(roleName, true, out var parsedRole))
			{
				return;
			}

			var role = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Role == parsedRole);
			if (role == null) return;

			var users = await _dbContext.Users
				.Where(u => EF.Property<Guid>(u, "RoleId") == role.RoleId)
				.ToListAsync();

			var notificationsToPush = new List<(Guid UserId, NotificationDto Dto)>();

			foreach (var user in users)
			{
				var notification = new Notification
				{
					NotificationId = Guid.NewGuid(),
					UserId = user.UserId,
					Message = message,
					IsRead = false,
					CreatedDate = DateTime.UtcNow
				};
				_dbContext.Notifications.Add(notification);

				var dto = new NotificationDto
				{
					NotificationId = notification.NotificationId,
					Message = notification.Message,
					IsRead = notification.IsRead,
					CreatedDate = DateTime.SpecifyKind(notification.CreatedDate, DateTimeKind.Utc)
				};
				notificationsToPush.Add((user.UserId, dto));
			}

			await _dbContext.SaveChangesAsync();

			// Push to user group real-time
			foreach (var item in notificationsToPush)
			{
				await _hubContext.Clients.Group(item.UserId.ToString()).SendAsync("ReceiveNotification", item.Dto);
			}
		}

		public async Task MarkAllAsReadAsync(Guid userId)
		{
			var unread = await _dbContext.Notifications
				.Where(n => n.UserId == userId && !n.IsRead)
				.ToListAsync();

			foreach (var n in unread)
			{
				n.IsRead = true;
			}

			await _dbContext.SaveChangesAsync();
		}

		public async Task SendRefreshSignalAsync(string dataArea)
		{
			await _hubContext.Clients.All.SendAsync("RefreshData", dataArea);
		}
	}
}
