namespace DoctorAppointmentSystem.Domain.Entities
{
	public class Patient
	{
		public Guid PatientId { get; set; }
		public Guid UserId { get; set; }
		public EBloodGroup BloodGroup { get; set; }
		public string EmergencyConactName { get; set; }
		public string EmergencyConactNumber { get; set; }
	}

	public enum EBloodGroup
	{

	}
}
