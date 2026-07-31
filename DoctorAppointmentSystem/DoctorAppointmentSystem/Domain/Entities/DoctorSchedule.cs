using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoctorAppointmentSystem.Domain.Entities
{
	/// <summary>
	/// Represents a doctor's weekly recurring availability schedule slot.
	/// Specifies start time, end time, days of week bitmask, and availability flags.
	/// </summary>
	[Table("DoctorSchedules")]
	public class DoctorSchedule
	{
		/// <summary>
		/// Gets or sets the unique primary key identifier for the schedule record.
		/// </summary>
		[Key]
		public Guid ScheduleId { get; set; }

		/// <summary>
		/// Gets or sets the doctor associated with this schedule slot.
		/// </summary>
		[Required]
		public Doctor Doctor { get; set; }

		/// <summary>
		/// Gets or sets the daily consultation start time span.
		/// </summary>
		[Required]
		public TimeSpan StartTime { get; set; }

		/// <summary>
		/// Gets or sets the daily consultation end time span.
		/// </summary>
		[Required]
		public TimeSpan EndTime { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the doctor is actively accepting bookings during this time window.
		/// </summary>
		[Required]
		public bool IsAvailable { get; set; }

		/// <summary>
		/// Gets or sets the days of week bitmask or integer representation (e.g. 1 = Monday, 2 = Tuesday).
		/// </summary>
		[Required]
		public int DaysOfWeek { get; set; }
	}
}
