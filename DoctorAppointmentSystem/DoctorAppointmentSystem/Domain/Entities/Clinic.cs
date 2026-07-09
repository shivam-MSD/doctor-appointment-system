using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoctorAppointmentSystem.Domain.Entities
{
	[Table("Clinics")]
	public class Clinic
	{
		[Key]
		public Guid ClinicId { get; set; }

		[Required]
		[MaxLength(150)]
		public string ClinicName { get; set; }

		[Required]
		[MaxLength(50)]
		public string ClinicType { get; set; } // Clinic, PhysiotherapyCenter, Hospital, etc.

		[Required]
		public Doctor Doctor { get; set; }

		[Required]
		public Address Address { get; set; }

		[Required]
		public EVerificationStatus VerificationStatus { get; set; } = EVerificationStatus.Pending;

		public string? RejectionReason { get; set; }

		public string? OpenDays { get; set; }

		public string? StartTime { get; set; }

		public string? EndTime { get; set; }

		public bool IsAvailable { get; set; } = true;

		public string? UnavailabilityReason { get; set; }

		public bool IsDoctorAvailable { get; set; } = true;

		public string? DoctorUnavailabilityReason { get; set; }

		public DateTime? BookingWindowEndDate { get; set; }
		public DateTime? BookingWindowStartDate { get; set; }
		public string? SupportedModes { get; set; }

		public Guid? ParentClinicId { get; set; }

		[Required]
		public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
	}
}
