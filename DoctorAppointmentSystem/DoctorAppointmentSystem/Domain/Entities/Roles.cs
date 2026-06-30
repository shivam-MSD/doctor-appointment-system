namespace DoctorAppointmentSystem.Domain.Entities
{
	public class Roles
	{
		public Guid RoleId { get; set; }
		public ERole Role { get; set; }
	}

	public enum ERole
	{
		SuperAdmin,
		Admin,
		Doctor,
		Patient
	}
}
