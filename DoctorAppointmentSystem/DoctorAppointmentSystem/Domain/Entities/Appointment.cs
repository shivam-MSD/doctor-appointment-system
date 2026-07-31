using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoctorAppointmentSystem.Domain.Entities
{
	/// <summary>
	/// Represents an appointment booking record between a patient and a doctor (at a clinic branch or virtual consultation).
	/// Holds appointment status, assigned time slots, queue positioning, medical notes, prescriptions, and reschedule states.
	/// </summary>
	[Table("Appointments")]
	public class Appointment
	{
		/// <summary>
		/// Gets or sets the unique primary key identifier for the appointment.
		/// </summary>
		[Key]
		public Guid AppointmentId { get; set; }

		/// <summary>
		/// Gets or sets the patient booking the appointment.
		/// </summary>
		[Required]
		public Patient Patient { get; set; }

		/// <summary>
		/// Gets or sets the doctor conducting the appointment consultation.
		/// </summary>
		[Required]
		public Doctor Doctor { get; set; }

		/// <summary>
		/// Gets or sets the clinic branch location where the in-person appointment takes place (null for online consultations).
		/// </summary>
		public Clinic? Clinic { get; set; }

		/// <summary>
		/// Gets or sets the scheduled calendar date of the appointment.
		/// </summary>
		[Required]
		public DateTime AppointmentDate { get; set; }

		/// <summary>
		/// Gets or sets the calculated or requested start time of the consultation.
		/// </summary>
		public DateTime? StartTime { get; set; }

		/// <summary>
		/// Gets or sets the calculated or requested end time of the consultation.
		/// </summary>
		public DateTime? EndTime { get; set; }

		/// <summary>
		/// Sequential queue position for this clinic on the appointment date. e.g. Patient is #3 for Monday.
		/// </summary>
		public int QueueNumber { get; set; } = 0;

		/// <summary>
		/// Time assigned by doctor/admin after the appointment is booked. Shown to patient on dashboard.
		/// </summary>
		public DateTime? DoctorAssignedTime { get; set; }

		/// <summary>
		/// Gets or sets the current lifecycle status of the appointment (Pending, Confirmed, Cancelled, Completed, Rejected, Expired, RescheduleProposed, FollowUpProposed).
		/// </summary>
		[Required]
		public EAppointmentStatus EAppointmentStatus { get; set; }

		/// <summary>
		/// Gets or sets the reason or symptom description provided by the patient upon booking.
		/// </summary>
		[MaxLength(4000)]
		public string? Reason { get; set; }

		/// <summary>
		/// Gets or sets the UTC timestamp when the appointment was created.
		/// </summary>
		[Required]
		public DateTime CreatedDate { get; set; }

		/// <summary>
		/// Gets or sets the consultation type mode (InPerson or VideoConsultation).
		/// </summary>
		[Required]
		public EConsultationType EConsultationType { get; set; }

		/// <summary>
		/// Gets or sets the clinical notes recorded by the doctor during or after the consultation.
		/// </summary>
		[MaxLength(1000)]
		public string? Comment { get; set; }

		/// <summary>
		/// Gets or sets the medical report, diagnosis, or prescription text issued by the doctor.
		/// </summary>
		[MaxLength(2000)]
		public string? Report { get; set; }

		/// <summary>
		/// Gets or sets the reason provided by the doctor or administrator if an appointment request is rejected.
		/// </summary>
		[MaxLength(500)]
		public string? RejectionReason { get; set; }

		/// <summary>
		/// Gets or sets the proposed new date if a reschedule request is initiated.
		/// </summary>
		public DateTime? RescheduleProposedDate { get; set; }

		/// <summary>
		/// Gets or sets the proposed new time slot if a reschedule request is initiated.
		/// </summary>
		public DateTime? RescheduleProposedTime { get; set; }

		/// <summary>
		/// Gets or sets the reason accompanying a reschedule proposal.
		/// </summary>
		[MaxLength(500)]
		public string? RescheduleReason { get; set; }

		/// <summary>
		/// Gets or sets the timestamp when the appointment was confirmed.
		/// </summary>
		public DateTime? ConfirmedDate { get; set; }

		/// <summary>
		/// Gets or sets the timestamp when a reschedule was proposed.
		/// </summary>
		public DateTime? RescheduleProposedAt { get; set; }

		/// <summary>
		/// Gets or sets the timestamp when the appointment was cancelled.
		/// </summary>
		public DateTime? CancelledDate { get; set; }

		/// <summary>
		/// Gets or sets the role or name of the entity who cancelled the appointment ("Patient", "Doctor", or "Admin").
		/// </summary>
		[MaxLength(50)]
		public string? CancelledBy { get; set; }
	}

	/// <summary>
	/// Enumeration representing consultation delivery modes.
	/// </summary>
	public enum EConsultationType
	{
		InPerson,
		VideoConsultation
	}

	/// <summary>
	/// Enumeration representing appointment status states.
	/// </summary>
	public enum EAppointmentStatus
	{
		Pending,
		Confirmed,
		Cancelled,
		Completed,
		Rejected,
		Expired,
		RescheduleProposed,
		FollowUpProposed
	}
}
