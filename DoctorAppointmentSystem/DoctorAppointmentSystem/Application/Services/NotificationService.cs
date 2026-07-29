using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
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
		private readonly IServiceProvider _serviceProvider;

		public NotificationService(
			ApplicationDbContext dbContext,
			IHubContext<NotificationHub> hubContext,
			IServiceProvider serviceProvider)
		{
			_dbContext = dbContext;
			_hubContext = hubContext;
			_serviceProvider = serviceProvider;
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
			_ = Task.Run(async () =>
			{
				try
				{
					using var scope = _serviceProvider.CreateScope();
					var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

					var notification = new Notification
					{
						NotificationId = Guid.NewGuid(),
						UserId = userId,
						Message = message,
						IsRead = false,
						CreatedDate = DateTime.UtcNow
					};

					dbContext.Notifications.Add(notification);
					await dbContext.SaveChangesAsync();

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
				catch (Exception ex)
				{
					Console.WriteLine($"[Notification Error] Failed to create notification for user {userId}: {ex.Message}");
				}
			});

			await Task.CompletedTask;
		}

		public async Task CreateNotificationForRoleAsync(string roleName, string message)
		{
			_ = Task.Run(async () =>
			{
				try
				{
					if (!Enum.TryParse<ERole>(roleName, true, out var parsedRole))
					{
						return;
					}

					using var scope = _serviceProvider.CreateScope();
					var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

					var role = await dbContext.Roles.FirstOrDefaultAsync(r => r.Role == parsedRole);
					if (role == null) return;

					var users = await dbContext.Users
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
						dbContext.Notifications.Add(notification);

						var dto = new NotificationDto
						{
							NotificationId = notification.NotificationId,
							Message = notification.Message,
							IsRead = notification.IsRead,
							CreatedDate = DateTime.SpecifyKind(notification.CreatedDate, DateTimeKind.Utc)
						};
						notificationsToPush.Add((user.UserId, dto));
					}

					await dbContext.SaveChangesAsync();

					// Push to user group real-time
					foreach (var item in notificationsToPush)
					{
						await _hubContext.Clients.Group(item.UserId.ToString()).SendAsync("ReceiveNotification", item.Dto);
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine($"[Notification Error] Failed to create notification for role {roleName}: {ex.Message}");
				}
			});

			await Task.CompletedTask;
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
