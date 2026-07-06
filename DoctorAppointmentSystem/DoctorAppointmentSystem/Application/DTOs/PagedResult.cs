namespace DoctorAppointmentSystem.Application.DTOs
{
	public class PagedResult<T>
	{
		public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
		public int TotalCount { get; set; }
		public int PageNumber { get; set; }
		public int PageSize { get; set; }
		public int TotalPages { get; set; }

		public PagedResult()
		{
		}

		public PagedResult(IEnumerable<T> items, int totalCount, int pageNumber, int pageSize)
		{
			Items = items;
			TotalCount = totalCount;
			PageNumber = pageNumber;
			PageSize = pageSize;
			TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
		}
	}
}
