using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace MARS_BE.Features.Users;

public static class UsersQueryableExtensions
{
    // Filtering & search
    public static IQueryable<ApplicationUser> ApplyFilters(
        this IQueryable<ApplicationUser> q, UsersQuery dto)
    {
        if (!dto.IncludeInactive)
            q = q.Where(u => u.IsActive);

        if (!string.IsNullOrWhiteSpace(dto.Search))
        {
            var s = dto.Search.Trim().ToLower();
            q = q.Where(u =>
                (u.Email != null && u.Email.ToLower().Contains(s)) ||
                (u.UserName != null && u.UserName.ToLower().Contains(s)) ||
                (u.NormalizedEmail != null && u.NormalizedEmail.ToLower().Contains(s)));
        }

        return q;
    }

    // Sorting (whitelist a few cols)
    public static IQueryable<ApplicationUser> ApplySorting(
        this IQueryable<ApplicationUser> q, UsersQuery dto)
    {
        var sortBy = (dto.SortBy ?? "email").ToLower();
        var asc = string.Equals(dto.SortDir, "desc", StringComparison.OrdinalIgnoreCase) ? false : true;

        Expression<Func<ApplicationUser, object>> key = sortBy switch
        {
            "username"  => u => u.UserName!,
            "firstname" => u => u.FirstName!,
            "lastname"  => u => u.LastName!,
            "active"    => u => u.IsActive,
            _           => u => u.Email! // default
        };

        q = asc ? q.OrderBy(key) : q.OrderByDescending(key);
        return q;
    }

    // Paging
    public static IQueryable<T> ApplyPaging<T>(
        this IQueryable<T> q, int page, int pageSize)
    {
        var p = Math.Max(1, page);
        var ps = Math.Clamp(pageSize, 1, 200);
        return q.Skip((p - 1) * ps).Take(ps);
    }
}