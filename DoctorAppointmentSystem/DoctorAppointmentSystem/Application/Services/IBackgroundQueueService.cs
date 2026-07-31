using System;
using System.Threading;
using System.Threading.Tasks;

namespace DoctorAppointmentSystem.Application.Services
{
	public interface IBackgroundQueueService
	{
		void QueueBackgroundWorkItem(Func<IServiceProvider, CancellationToken, ValueTask> workItem);
		Task<Func<IServiceProvider, CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken);
	}
}
