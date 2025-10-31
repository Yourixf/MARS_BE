namespace MARS_BE.Features.Employees;

public sealed class EmployeeReadDto
{
    public Guid Id { get; init; }
    public string EmployeeNo { get; init; } = "";
    public string FirstName { get; init; } = "";
    public string LastName  { get; init; } = "";
    public string Email     { get; init; } = "";
    public DateTime HireDate { get; init; }
    public bool IsActive { get; init; }
}

public sealed class EmployeeCreateDto
{
    public string EmployeeNo { get; init; } = "";
    public string FirstName { get; init; } = "";
    public string LastName  { get; init; } = "";
    public string Email     { get; init; } = "";
    public DateTime HireDate { get; init; }
}

public sealed class EmployeeUpdateDto
{
    public string? FirstName { get; init; }
    public string? LastName  { get; init; }
    public string? Email     { get; init; }
    public DateTime? HireDate { get; init; }
    public bool? IsActive { get; init; }
}