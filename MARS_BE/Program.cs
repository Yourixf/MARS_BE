using FluentValidation;
using FluentValidation.AspNetCore;
using MARS_BE.Data;
using MARS_BE.Features.Employees;
using Microsoft.EntityFrameworkCore;
using MARS_BE.Common.Errors;


namespace MARS_BE;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var conn = builder.Configuration.GetConnectionString("Default")
                   ?? throw new InvalidOperationException("ConnectionStrings:Default missing");
        builder.Services.AddDbContext<AppDbContext>(opt => opt.UseNpgsql(conn));

        // Controllers (niet-short-hand)
        builder.Services.AddControllers();
        
        builder.Services.AddScoped<IEmployeesService, EmployeesService>();
        
        // Swagger
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // FluentValidation: scan assembly
        builder.Services.AddFluentValidationAutoValidation();
        builder.Services.AddValidatorsFromAssemblyContaining<Program>();
        
        builder.Services.AddAutoMapper(typeof(Program).Assembly);
        builder.Services.AddProblemDetailsPolicies();


        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();

            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.Migrate();
        }
        app.UseAppExceptionHandler();

        app.UseHttpsRedirection();
        
        app.MapControllers();

        app.Run();
    }
}