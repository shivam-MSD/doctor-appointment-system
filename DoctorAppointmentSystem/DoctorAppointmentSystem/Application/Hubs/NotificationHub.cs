using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace DoctorAppointmentSystem.Application.Hubs
{
	public class NotificationHub : Hub
	{
		public override async Task OnConnectedAsync()
		{
			var userIdStr = Context.GetHttpContext()?.Request.Query["userId"];
			if (Guid.TryParse(userIdStr, out var userId))
			{
				// Add connection to a group named after the UserId
				await Groups.AddToGroupAsync(Context.ConnectionId, userId.ToString());
			}
			await base.OnConnectedAsync();
		}

		public override async Task OnDisconnectedAsync(Exception? exception)
		{
			var userIdStr = Context.GetHttpContext()?.Request.Query["userId"];
			if (Guid.TryParse(userIdStr, out var userId))
			{
				await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId.ToString());
			}
			await base.OnDisconnectedAsync(exception);
		}
	}
}
