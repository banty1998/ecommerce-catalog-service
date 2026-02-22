using System.ComponentModel.DataAnnotations;

namespace ProductAPI.Models.Pagination
{
    public record PaginationQuery
    {
        public string? SearchTerm { get; init; }
        public string? SortColumn { get; init; }
        public bool SortDescending { get; init; } = false;

        [Range(1, int.MaxValue, ErrorMessage = "Page number must be greater than 0.")]
        public int PageNumber { get; init; } = 1;
        [Range(1, 100, ErrorMessage = "Page size must be between 1 and 100.")]
        public int PageSize { get; init; } = 10;
    }
}
