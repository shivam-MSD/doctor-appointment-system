using System.Threading.Tasks;

namespace DoctorAppointmentSystem.Application.Services
{
	public interface IWebPushService
	{
		string GetVapidPublicKey();
		Task SaveSubscriptionAsync(string userId, string endpoint, string p256dh, string auth);
		Task SendPushNotificationAsync(string userId, string title, string message, string portalUrl = "https://healsync-medical.web.app/patient/dashboard");
	}
}
