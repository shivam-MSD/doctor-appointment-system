using System.Threading.Tasks;

namespace DoctorAppointmentSystem.Application.Services
{
	public interface IHangfireJobService
	{
		void EnqueueAppointmentEmail(
			string toEmail,
			string subject,
			string title,
			string message,
			string doctorName,
			string dateStr,
			string timeOrStatus,
			string clinicName,
			string clinicAddress,
			string? patientName = null,
			string? comment = null,
			string? report = null,
			string? followUpStr = null);

		void EnqueueNotification(string userId, string message);
	}
}
