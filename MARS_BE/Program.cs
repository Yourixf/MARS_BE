using FluentValidation;
using FluentValidation.AspNetCore;
using MARS_BE.Data;
using MARS_BE.Features.Employees;
using Microsoft.EntityFrameworkCore;
using MARS_BE.Common.Errors;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using MARS_BE.Features.Users;
using MARS_BE.Infrastructure.Auth;
using MARS_BE.Common.Auth;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new() { Title = "MARS API", Version = "v1" });

            // JWT Bearer support
            c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                Description = "Enter (no Bearer prefix): '{token}'"
            });

            c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
            {
                {
                    new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                    {
                        Reference = new Microsoft.OpenApi.Models.OpenApiReference
                        {
                            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        // FluentValidation: scan assembly
        builder.Services.AddFluentValidationAutoValidation();
        builder.Services.AddValidatorsFromAssemblyContaining<Program>();
        
        builder.Services.AddAutoMapper(typeof(Program).Assembly);
        builder.Services.AddProblemDetailsPolicies();

        
        // Bind JwtOptions
        builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

        var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()!;

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                    ClockSkew = TimeSpan.Zero
                };

                o.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = ctx =>
                    {
                        Console.WriteLine($"JWT auth failed: {ctx.Exception.GetType().Name} - {ctx.Exception.Message}");
                        return Task.CompletedTask;
                    }
                };
            });

        
        // Identity (no UI)
        builder.Services.AddIdentityCore<ApplicationUser>(opt =>
            {
                opt.Password.RequiredLength = 8;
                opt.Password.RequireNonAlphanumeric = false;
                opt.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AppDbContext>();

        // Authorization policies (RBAC via "perm" claims)
        builder.Services.AddAuthorization(opt =>
        {
            opt.AddPolicy(Permissions.EmployeesRead,  p => p.RequireClaim("perm", Permissions.EmployeesRead));
            opt.AddPolicy(Permissions.EmployeesWrite, p => p.RequireClaim("perm", Permissions.EmployeesWrite));
            opt.AddPolicy(Permissions.ClientsRead,    p => p.RequireClaim("perm", Permissions.ClientsRead));
            opt.AddPolicy(Permissions.ClientsWrite,   p => p.RequireClaim("perm", Permissions.ClientsWrite));
        });

        // Token service
        builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();


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
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseHttpsRedirection();
        app.MapControllers();
        app.Run();
    }
}