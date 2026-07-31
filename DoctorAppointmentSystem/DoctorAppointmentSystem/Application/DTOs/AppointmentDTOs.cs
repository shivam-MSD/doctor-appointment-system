using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DoctorAppointmentSystem.Application.DTOs
{
	/// <summary>
	/// Data transfer object representing full appointment details returned to patients, doctors, and clinic admins.
	/// </summary>
	public class AppointmentDto
	{
		/// <summary>Gets or sets the appointment ID.</summary>
		public Guid AppointmentId { get; set; }
		/// <summary>Gets or sets the patient ID.</summary>
		public Guid PatientId { get; set; }
		/// <summary>Gets or sets the patient full name.</summary>
		public string PatientName { get; set; }
		/// <summary>Gets or sets the patient age in years.</summary>
		public int PatientAge { get; set; }
		/// <summary>Gets or sets the patient gender string.</summary>
		public string PatientGender { get; set; }
		/// <summary>Gets or sets the doctor ID.</summary>
		public Guid DoctorId { get; set; }
		/// <summary>Gets or sets the doctor full name.</summary>
		public string DoctorName { get; set; }
		/// <summary>Gets or sets the doctor specialization name.</summary>
		public string DoctorSpecialization { get; set; }
		/// <summary>Gets or sets the clinic branch ID (if in-person).</summary>
		public Guid? ClinicId { get; set; }
		/// <summary>Gets or sets the clinic branch name.</summary>
		public string? ClinicName { get; set; }
		/// <summary>Gets or sets the appointment date.</summary>
		public DateTime AppointmentDate { get; set; }
		/// <summary>Gets or sets the consultation start time.</summary>
		public DateTime? StartTime { get; set; }
		/// <summary>Gets or sets the consultation end time.</summary>
		public DateTime? EndTime { get; set; }
		/// <summary>Gets or sets the appointment status string.</summary>
		public string Status { get; set; }
		/// <summary>Gets or sets the patient reason string.</summary>
		public string Reason { get; set; }
		/// <summary>Gets or sets the consultation type (InPerson or VideoConsultation).</summary>
		public string ConsultationType { get; set; }
		/// <summary>Gets or sets the UTC creation date.</summary>
		public DateTime CreatedDate { get; set; }
		/// <summary>Gets or sets clinical comments by the doctor.</summary>
		public string? Comment { get; set; }
		/// <summary>Gets or sets medical report / prescription text.</summary>
		public string? Report { get; set; }
		/// <summary>Gets or sets rejection reason if rejected.</summary>
		public string? RejectionReason { get; set; }
		/// <summary>Gets or sets sequential queue position for this clinic on the appointment date.</summary>
		public int QueueNumber { get; set; }
		/// <summary>Gets or sets approximate consultation time assigned by the doctor/admin.</summary>
		public DateTime? DoctorAssignedTime { get; set; }
		/// <summary>Gets or sets proposed reschedule date.</summary>
		public DateTime? RescheduleProposedDate { get; set; }
		/// <summary>Gets or sets proposed reschedule time.</summary>
		public DateTime? RescheduleProposedTime { get; set; }
		/// <summary>Gets or sets reason accompanying reschedule proposal.</summary>
		public string? RescheduleReason { get; set; }
		/// <summary>Gets or sets confirmation timestamp.</summary>
		public DateTime? ConfirmedDate { get; set; }
		/// <summary>Gets or sets timestamp when reschedule was proposed.</summary>
		public DateTime? RescheduleProposedAt { get; set; }
		/// <summary>Gets or sets cancellation timestamp.</summary>
		public DateTime? CancelledDate { get; set; }
		/// <summary>Gets or sets entity role who cancelled the appointment.</summary>
		public string? CancelledBy { get; set; }
	}

	/// <summary>
	/// Data transfer object carrying data payload to book a new appointment.
	/// </summary>
	public class CreateAppointmentDto
	{
		/// <summary>Gets or sets the target patient ID.</summary>
		[Required]
		public Guid PatientId { get; set; }

		/// <summary>Gets or sets the target doctor ID.</summary>
		[Required]
		public Guid DoctorId { get; set; }

		/// <summary>Gets or sets the target clinic branch ID (optional for video call).</summary>
		public Guid? ClinicId { get; set; }

		/// <summary>Gets or sets the appointment booking date.</summary>
		[Required]
		public DateTime AppointmentDate { get; set; }

		/// <summary>Gets or sets the symptom/reason description.</summary>
		[MaxLength(4000, ErrorMessage = "Reason cannot exceed 4000 characters.")]
		public string? Reason { get; set; } = string.Empty;

		/// <summary>Gets or sets consultation mode ("InPerson", "VideoConsultation").</summary>
		[Required]
		public string ConsultationType { get; set; }
	}

	/// <summary>
	/// Data transfer object to update appointment status.
	/// </summary>
	public class UpdateAppointmentStatusDto
	{
		/// <summary>Gets or sets the new status string.</summary>
		[Required]
		public string Status { get; set; }
	}

	/// <summary>
	/// Data transfer object containing doctor profile details and past consultation history for recently consulted doctors.
	/// </summary>
	public class ConsultedDoctorDto
	{
		/// <summary>Gets or sets doctor ID.</summary>
		public Guid DoctorId { get; set; }
		/// <summary>Gets or sets doctor full name.</summary>
		public string DoctorName { get; set; }
		/// <summary>Gets or sets specialization name.</summary>
		public string Specialization { get; set; }
		/// <summary>Gets or sets consultation fee.</summary>
		public double ConsultationFee { get; set; }
		/// <summary>Gets or sets about doctor summary.</summary>
		public string? AboutDoctor { get; set; }
		/// <summary>Gets or sets doctor age.</summary>
		public int Age { get; set; }
		/// <summary>Gets or sets years of experience.</summary>
		public int YearsOfExperience { get; set; }
		/// <summary>Gets or sets qualification string.</summary>
		public string Qualification { get; set; }
		/// <summary>Gets or sets medical license number.</summary>
		public string LicenceNumber { get; set; }
		/// <summary>Gets or sets total completed consultations count.</summary>
		public int CompletedConsultationsCount { get; set; }
		/// <summary>Gets or sets list of associated clinic branches.</summary>
		public IEnumerable<ClinicBasicDto> Clinics { get; set; } = Enumerable.Empty<ClinicBasicDto>();
		/// <summary>Gets or sets list of past appointments conducted with this doctor.</summary>
		public IEnumerable<AppointmentDto> Appointments { get; set; } = Enumerable.Empty<AppointmentDto>();
	}

	/// <summary>
	/// Data transfer object carrying approval comment and assigned time slot for an appointment.
	/// </summary>
	public class ApproveAppointmentDto
	{
		/// <summary>Gets or sets approval comment.</summary>
		public string? Comment { get; set; }
		/// <summary>Gets or sets assigned consultation time.</summary>
		public DateTime? DoctorAssignedTime { get; set; }
	}

	/// <summary>
	/// Data transfer object carrying rejection reason for an appointment request.
	/// </summary>
	public class RejectAppointmentDto
	{
		/// <summary>Gets or sets rejection reason explanation.</summary>
		[Required]
		public string Reason { get; set; }
	}

	/// <summary>
	/// Data transfer object carrying completion notes, medical reports, and optional follow-up booking details.
	/// </summary>
	public class CompleteAppointmentDto
	{
		/// <summary>Gets or sets clinical consultation notes.</summary>
		public string? Comment { get; set; }
		/// <summary>Gets or sets medical report / prescription text.</summary>
		public string? Report { get; set; }
		/// <summary>Gets or sets follow-up appointment booking payload.</summary>
		public CreateFollowUpDto? FollowUp { get; set; }
	}

	/// <summary>
	/// Data transfer object to move appointment back to pending queue.
	/// </summary>
	public class MovePendingAppointmentDto
	{
		/// <summary>Gets or sets explanation comment.</summary>
		public string? Comment { get; set; }
	}

	/// <summary>
	/// Data transfer object showing daily clinic appointment booking capacity.
	/// </summary>
	public class DayAvailabilityDto
	{
		/// <summary>Gets or sets calendar date.</summary>
		public DateTime Date { get; set; }
		/// <summary>Gets or sets clinic branch ID.</summary>
		public Guid ClinicId { get; set; }
		/// <summary>Gets or sets booked appointments count.</summary>
		public int BookedCount { get; set; }
		/// <summary>Gets or sets maximum capacity limit (null = unlimited).</summary>
		public int? MaxCapacity { get; set; }
		/// <summary>Gets or sets remaining available booking slots.</summary>
		public int? RemainingSlots { get; set; }
		/// <summary>Gets or sets a value indicating whether daily capacity is reached.</summary>
		public bool IsFull { get; set; }
	}

	/// <summary>
	/// Data transfer object to assign or update a consultation time slot.
	/// </summary>
	public class AssignAppointmentTimeDto
	{
		/// <summary>Gets or sets doctor assigned consultation time.</summary>
		[Required]
		public DateTime DoctorAssignedTime { get; set; }
		/// <summary>Gets or sets comment.</summary>
		public string? Comment { get; set; }
	}

	/// <summary>
	/// Data transfer object carrying reschedule proposal date, time, and reason.
	/// </summary>
	public class ProposeRescheduleDto
	{
		/// <summary>Gets or sets appointment ID.</summary>
		[Required]
		public Guid AppointmentId { get; set; }
		/// <summary>Gets or sets proposed new date.</summary>
		[Required]
		public DateTime ProposedDate { get; set; }
		/// <summary>Gets or sets proposed new time.</summary>
		public DateTime? ProposedTime { get; set; }
		/// <summary>Gets or sets reschedule reason explanation.</summary>
		[Required]
		public string Reason { get; set; }
	}

	/// <summary>
	/// Data transfer object carrying response (accept/decline) to a proposed reschedule.
	/// </summary>
	public class RespondRescheduleDto
	{
		/// <summary>Gets or sets appointment ID.</summary>
		[Required]
		public Guid AppointmentId { get; set; }
		/// <summary>Gets or sets accept decision boolean (true = accept, false = decline).</summary>
		[Required]
		public bool Accept { get; set; }
	}

	/// <summary>
	/// Data transfer object representing an appointment audit log entry.
	/// </summary>
	public class AppointmentAuditLogDto
	{
		/// <summary>Gets or sets audit log ID.</summary>
		public Guid LogId { get; set; }
		/// <summary>Gets or sets appointment ID.</summary>
		public Guid AppointmentId { get; set; }
		/// <summary>Gets or sets patient full name.</summary>
		public string? PatientName { get; set; }
		/// <summary>Gets or sets action type string.</summary>
		public string Action { get; set; }
		/// <summary>Gets or sets event UTC timestamp.</summary>
		public DateTime Timestamp { get; set; }
		/// <summary>Gets or sets actor display name.</summary>
		public string? ActorName { get; set; }
		/// <summary>Gets or sets actor role string.</summary>
		public string? ActorRole { get; set; }
		/// <summary>Gets or sets notes or contextual details.</summary>
		public string? Notes { get; set; }
	}

	/// <summary>
	/// Data transfer object to create a follow-up appointment during consultation completion.
	/// </summary>
	public class CreateFollowUpDto
	{
		/// <summary>Gets or sets clinic branch ID.</summary>
		[Required]
		public Guid ClinicId { get; set; }
		/// <summary>Gets or sets follow-up appointment date.</summary>
		[Required]
		public DateTime AppointmentDate { get; set; }
		/// <summary>Gets or sets start time string.</summary>
		[Required]
		public string StartTime { get; set; }
		/// <summary>Gets or sets end time string.</summary>
		[Required]
		public string EndTime { get; set; }
		/// <summary>Gets or sets consultation mode ("InPerson" or "VideoConsultation").</summary>
		[Required]
		public string ConsultationType { get; set; }
	}
}
