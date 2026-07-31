using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DoctorAppointmentSystem.Application.Services
{
	public class BackgroundQueueService : IBackgroundQueueService
	{
		private readonly Channel<Func<IServiceProvider, CancellationToken, ValueTask>> _queue;

		public BackgroundQueueService()
		{
			var options = new UnboundedChannelOptions
			{
				SingleReader = true
			};
			_queue = Channel.CreateUnbounded<Func<IServiceProvider, CancellationToken, ValueTask>>(options);
		}

		public void QueueBackgroundWorkItem(Func<IServiceProvider, CancellationToken, ValueTask> workItem)
		{
			if (workItem == null) throw new ArgumentNullException(nameof(workItem));
			_queue.Writer.TryWrite(workItem);
		}

		public async Task<Func<IServiceProvider, CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken)
		{
			return await _queue.Reader.ReadAsync(cancellationToken);
		}
	}

	public class BackgroundQueueHostedService : BackgroundService
	{
		private readonly IBackgroundQueueService _taskQueue;
		private readonly IServiceProvider _serviceProvider;
		private readonly ILogger<BackgroundQueueHostedService> _logger;

		public BackgroundQueueHostedService(
			IBackgroundQueueService taskQueue,
			IServiceProvider serviceProvider,
			ILogger<BackgroundQueueHostedService> logger)
		{
			_taskQueue = taskQueue;
			_serviceProvider = serviceProvider;
			_logger = logger;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				try
				{
					var workItem = await _taskQueue.DequeueAsync(stoppingToken);

					using var scope = _serviceProvider.CreateScope();
					await workItem(scope.ServiceProvider, stoppingToken);
				}
				catch (OperationCanceledException)
				{
					// Execution stopping
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error occurred executing background queued work item.");
				}
			}
		}
	}
}
