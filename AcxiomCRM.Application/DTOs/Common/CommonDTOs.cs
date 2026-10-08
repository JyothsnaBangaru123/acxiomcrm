namespace AcxiomCRM.Application.DTOs.Common;

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = new List<T>();
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    public PagedResult() { }

    public PagedResult(IReadOnlyList<T> items, int totalCount, int pageNumber, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        PageNumber = pageNumber < 1 ? 1 : pageNumber;
        PageSize = pageSize < 1 ? 10 : pageSize;
    }
}

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new();

    public static ApiResponse<T> Ok(T data, string? message = null) => new() { Success = true, Data = data, Message = message };
    public static ApiResponse<T> Fail(string error, List<string>? errors = null)
    {
        var response = new ApiResponse<T> { Success = false, Message = error };
        if (errors != null) response.Errors.AddRange(errors);
        else response.Errors.Add(error);
        return response;
    }
}

public class UserContext
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? IpAddress { get; set; }

    public bool IsAdmin => Role == Domain.Enums.AppRoles.Admin;
    public bool IsManager => Role == Domain.Enums.AppRoles.Manager;
    public bool IsSalesExecutive => Role == Domain.Enums.AppRoles.SalesExecutive;
}
