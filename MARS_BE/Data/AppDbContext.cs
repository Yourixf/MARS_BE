using MARS_BE.Features.Employees;
using Microsoft.EntityFrameworkCore;

namespace MARS_BE.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Employee> Employees => Set<Employee>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<Employee>(e =>
        {
            e.HasIndex(x => x.EmployeeNo).IsUnique();
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.FirstName).HasMaxLength(100);
            e.Property(x => x.LastName).HasMaxLength(100);
            e.Property(x => x.Extras).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        });

        b.Entity<Employee>().HasQueryFilter(e => e.IsActive);
    }
}