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
			try
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
				await _hubContext.Clients.All.SendAsync("RefreshData", "Appointments");

				// Dispatch background Web Push Lockscreen alert for mobile devices (when app is closed)
				var webPushService = _serviceProvider.GetService<IWebPushService>();
				if (webPushService != null)
				{
					await webPushService.SendPushNotificationAsync(userId.ToString(), "HealSync Appointment Alert", message);
				}

				// Dispatch WhatsApp Alert to Patient, Doctor, or Admin if mobile number exists
				var whatsAppService = _serviceProvider.GetService<IWhatsAppService>();
				if (whatsAppService != null)
				{
					// 1. Check Patient mobile
					var userPatient = await _dbContext.UserPatients
						.Include(up => up.Patient)
						.FirstOrDefaultAsync(up => up.UserId == userId && up.RelationshipType == ERelationshipType.Self);

					if (userPatient?.Patient?.MobileNo != null)
					{
						await whatsAppService.SendWhatsAppAlertAsync(userPatient.Patient.MobileNo, message);
					}
					else
					{
						// 2. Check Doctor mobile
						var doctor = await _dbContext.Doctors.FirstOrDefaultAsync(d => EF.Property<Guid>(d, "UserId") == userId);
						if (doctor?.MobileNo != null)
						{
							await whatsAppService.SendWhatsAppAlertAsync(doctor.MobileNo, message);
						}
						else
						{
							// 3. Admin mobile check
							var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserId == userId);
							if (user?.Email != null)
							{
								Console.WriteLine($"[Admin Notification] User {user.Email}: {message}");
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[Notification Error] Failed to create notification for user {userId}: {ex.Message}");
			}
		}

		public async Task CreateNotificationForRoleAsync(string roleName, string message)
		{
			try
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
				var whatsAppService = _serviceProvider.GetService<IWhatsAppService>();

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

					// Dispatch WhatsApp alert to role members
					if (whatsAppService != null)
					{
						var doc = await _dbContext.Doctors.FirstOrDefaultAsync(d => EF.Property<Guid>(d, "UserId") == user.UserId);
						if (doc?.MobileNo != null)
						{
							await whatsAppService.SendWhatsAppAlertAsync(doc.MobileNo, message);
						}
					}
				}

				await _dbContext.SaveChangesAsync();

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
