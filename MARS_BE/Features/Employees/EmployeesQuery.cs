namespace MARS_BE.Features.Employees;

public sealed class EmployeesQuery
{
    public string? Search { get; init; }
    public string? FirstName { get; init; }
    public string? LastName  { get; init; }
    public string? Email     { get; init; }

    public bool IncludeInactive { get; init; } = false;

    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;

    public string? SortBy { get; init; }   // e.g. lastName, firstName, hireDate, email
    public string? SortDir { get; init; }  // asc | desc
    
    // Custom database fields
    public string? ExtraKey { get; init; }
    public string? ExtraEquals { get; init; }

}