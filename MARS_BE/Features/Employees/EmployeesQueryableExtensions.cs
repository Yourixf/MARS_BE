using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace MARS_BE.Features.Employees;

internal static class EmployeesQueryableExtensions
{
    // Case-insensitive helper (Postgres ILIKE als beschikbaar; anders ToLower)
    private static IQueryable<Employee> WhereIlike(this IQueryable<Employee> q,
        Func<Employee, string?> selector, string value)
    {
        // Als je Npgsql gebruikt, kun je EF.Functions.ILike gebruiken:
        // return q.Where(e => EF.Functions.ILike(selector(e)!, $"%{value}%"));
        var v = value.ToLowerInvariant();
        return q.Where(e => (selector(e) ?? "").ToLower().Contains(v));
    }

    public static IQueryable<Employee> ApplyFilters(this IQueryable<Employee> q, EmployeesQuery f)
    {
        if (f.IncludeInactive)
            q = q.IgnoreQueryFilters();

        if (!string.IsNullOrWhiteSpace(f.Search))
        {
            var s = f.Search;
            q = q.Where(e =>
                e.EmployeeNo.Contains(s) ||
                e.FirstName.Contains(s)  ||
                e.LastName.Contains(s)   ||
                e.Email.Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(f.FirstName))
            q = q.WhereIlike(e => e.FirstName, f.FirstName);

        if (!string.IsNullOrWhiteSpace(f.LastName))
            q = q.WhereIlike(e => e.LastName, f.LastName);

        if (!string.IsNullOrWhiteSpace(f.Email))
            q = q.WhereIlike(e => e.Email, f.Email);

        return q;
    }

    public static IQueryable<Employee> ApplySorting(this IQueryable<Employee> q, EmployeesQuery f)
    {
        var dirDesc = string.Equals(f.SortDir, "desc", StringComparison.OrdinalIgnoreCase);

        // default sort
        Expression<Func<Employee, object>> key1 = e => e.LastName;
        Expression<Func<Employee, object>> key2 = e => e.FirstName;

        if (!string.IsNullOrWhiteSpace(f.SortBy))
        {
            switch (f.SortBy.ToLowerInvariant())
            {
                case "firstname": key1 = e => e.FirstName; key2 = e => e.LastName; break;
                case "hiredate":  key1 = e => e.HireDate;  key2 = e => e.LastName; break;
                case "email":     key1 = e => e.Email!;    key2 = e => e.LastName; break;
                case "employeeno":key1 = e => e.EmployeeNo;key2 = e => e.LastName; break;
                // default blijft lastName, firstName
            }
        }

        q = dirDesc
            ? q.OrderByDescending(key1).ThenByDescending(key2)
            : q.OrderBy(key1).ThenBy(key2);

        return q;
    }

    public static IQueryable<T> ApplyPaging<T>(this IQueryable<T> q, int page, int pageSize)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0 || pageSize > 200) pageSize = 20;
        return q.Skip((page - 1) * pageSize).Take(pageSize);
    }

    public static IQueryable<EmployeeReadDto> ProjectToReadDto(this IQueryable<Employee> q)
        => q.Select(e => new EmployeeReadDto
        {
            Id         = e.Id,
            EmployeeNo = e.EmployeeNo,
            FirstName  = e.FirstName,
            LastName   = e.LastName,
            Email      = e.Email,
            HireDate   = e.HireDate,
            IsActive   = e.IsActive
        });
}
