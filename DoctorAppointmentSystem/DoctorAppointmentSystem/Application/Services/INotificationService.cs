using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DoctorAppointmentSystem.Application.DTOs;

namespace DoctorAppointmentSystem.Application.Services
{
	public interface INotificationService
	{
		Task<IEnumerable<NotificationDto>> GetNotificationsForUserAsync(Guid userId);
		Task CreateNotificationAsync(Guid userId, string message);
		Task CreateNotificationForRoleAsync(string roleName, string message);
		Task MarkAllAsReadAsync(Guid userId);
	}
}
