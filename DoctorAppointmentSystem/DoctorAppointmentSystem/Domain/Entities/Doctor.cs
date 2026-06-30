namespace DoctorAppointmentSystem.Domain.Entities
{
	public class Doctor
	{
		public Guid UserId { get; set; }
		public Guid DoctorId { get; set; }
		public Guid SpecializationId { get; set; }
		public string Qualification {  get; set; }
		public int YearsOfExperience { get; set; }
		public string LicenceNumber { get; set; }
		public double ConsultationFee {  get; set; }
		public string HospitalName { get; set; }
		public EVerificationStatus VerificationStatus { get; set; }
		public byte[] ProfileImage {  get; set; }
		public string AboutDoctor {  get; set; }
	}

	public enum EVerificationStatus
	{
		Verified,
		Pending
	}
}
