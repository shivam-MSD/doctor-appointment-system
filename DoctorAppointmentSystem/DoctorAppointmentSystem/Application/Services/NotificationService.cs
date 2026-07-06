using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DoctorAppointmentSystem.Application.DTOs;
using DoctorAppointmentSystem.Domain.Entities;
using DoctorAppointmentSystem.Persistent.Context;

namespace DoctorAppointmentSystem.Application.Services
{
	public class NotificationService : INotificationService
	{
		private readonly ApplicationDbContext _dbContext;

		public NotificationService(ApplicationDbContext dbContext)
		{
			_dbContext = dbContext;
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
					CreatedDate = n.CreatedDate
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
			}

			await _dbContext.SaveChangesAsync();
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
	}
}
