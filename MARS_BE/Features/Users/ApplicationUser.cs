using Microsoft.AspNetCore.Identity;

namespace MARS_BE.Features.Users;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    // Multi-tenant ready, even if we use it later
    public Guid TenantId { get; set; } = Guid.Empty;

    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    // Optional 1-1 mapping to Employee later:
    public Guid? EmployeeId { get; set; }
}