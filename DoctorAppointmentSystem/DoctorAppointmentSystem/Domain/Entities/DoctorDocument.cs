namespace DoctorAppointmentSystem.Domain.Entities
{
	public class DoctorDocument
	{
		public Guid DocumentId { get; set; }
		public Guid DoctorId { get; set; }
		public string DocumentType { get; set; }
		public DateTime UploadedDate {  get; set; }
		public string Status {  get; set; }
		public string Path { get; set; }
	}
}
