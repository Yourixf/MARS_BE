namespace MARS_BE.Features.Users;

public sealed class UsersQuery
{
    public string? Search { get; set; }
    public bool IncludeInactive { get; set; } = false;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public string? SortBy { get; set; } 
    public string? SortDir { get; set; }
}

public sealed class UserReadDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = default!;
    public string? UserName { get; set; }
    public string? FirstName { get; set; }
    public string? LastName  { get; set; }
    public bool IsActive { get; set; } 
}

public sealed class UserCreateDto
{
    public string Email { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string? FirstName { get; set; }
    public string? LastName  { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class UserUpdateDto
{
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName  { get; set; }
    public bool? IsActive { get; set; }
}