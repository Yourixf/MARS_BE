// using-directives:
using MARS_BE.Features.Employees;
using MARS_BE.Features.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MARS_BE.Data;

public sealed class AppDbContext
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b); // IMPORTANT for Identity tables

        b.Entity<Employee>(e =>
        {
            e.HasIndex(x => x.EmployeeNo).IsUnique();
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.FirstName).HasMaxLength(100);
            e.Property(x => x.LastName).HasMaxLength(100);
            e.Property(x => x.Extras).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        });

        b.Entity<Employee>().HasQueryFilter(e => e.IsActive);

        b.Entity<RefreshToken>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Token).IsRequired();
            e.HasIndex(x => new { x.UserId, x.Token }).IsUnique();
        });
    }
}