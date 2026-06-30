namespace DoctorAppointmentSystem.Domain.Entities
{
	public class Address
	{
		public Guid AddressId { get; set; }
		public Guid UserId { get; set; }
		public string Country {  get; set; }
		public string State {  get; set; }
		public string City { get; set; }
		public string Area { get; set; }	
		public string Addressline1 { get; set; }
		public string Addressline2 { get; set; }
	}
}
