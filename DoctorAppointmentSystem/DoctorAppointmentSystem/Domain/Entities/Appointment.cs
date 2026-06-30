namespace DoctorAppointmentSystem.Domain.Entities
{
	public class Appointment
	{
		public Guid AppointmentId {  get; set; }
		public Guid PatientId {  get; set; }
		public Guid DoctorId { get; set; }
		public DateTime AppointmentDate { get; set; }
		public DateTime StartTime {  get; set; }
		public DateTime EndTime {  get; set; }
		public EAppointmentStatus EAppointmentStatus { get; set; }
		public string Reason { get; set; }
		public DateTime CreatedDate { get; set; }
		public EConsultationType EConsultationType { get; set; }
	}

	public enum EConsultationType
	{

	}

	public enum EAppointmentStatus
	{
		Pending,
		Confirmed,
		Cancelled,
		Completed
	}
}
