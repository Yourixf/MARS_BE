using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

using MARS_BE.Data;
using MARS_BE.Features.Users;
using MARS_BE.Infrastructure.Auth;

// Starts the configuration of the app
var builder = WebApplication.CreateBuilder(args);

// Register controllers (API endpoints)
builder.Services.AddControllers();

// Register user service layer
builder.Services.AddScoped<IUsersService, UsersService>();

// AutoMapper: scan current assembly for Profile classes
builder.Services.AddAutoMapper(typeof(Program));

// Swagger (OpenAPI) with Bearer auth support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "MARS API", Version = "v1" });

    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste your access token here (without the 'Bearer ' prefix)."
    };

    c.AddSecurityDefinition("Bearer", scheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { scheme, new List<string>() }
    });
});

// DbContext (uses 'Default' connection string from appsettings.Development.json)
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// Identity (Guid keys) + EF stores + SignInManager
builder.Services
    .AddIdentityCore<ApplicationUser>(opt =>
    {
        opt.User.RequireUniqueEmail = true;
        opt.Password.RequiredLength = 6;           // dev-friendly password policy
        opt.Password.RequireNonAlphanumeric = false;
        opt.Password.RequireUppercase = false;
        opt.Password.RequireLowercase = false;
        opt.Password.RequireDigit = false;
        // (optional) configure lockout settings here
    })
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddSignInManager(); // required because AuthController uses SignInManager

// Provides access to HttpContext for SignInManager and other services
builder.Services.AddHttpContextAccessor();

// Authorization policies (currently "allow all" for development)
builder.Services.AddAuthorization(opt =>
{
    opt.AddPolicy("employees.read",  p => p.RequireAssertion(_ => true));
    opt.AddPolicy("employees.write", p => p.RequireAssertion(_ => true));
});

// Bind JwtOptions from configuration (for JwtTokenService via IOptions<JwtOptions>)
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

// JWT bearer validation (reads validation settings directly from configuration)
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(o =>
{
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer           = true,
        ValidateAudience         = true,
        ValidateIssuerSigningKey = true,
        ValidateLifetime         = true,
        ValidIssuer              = builder.Configuration["Jwt:Issuer"],
        ValidAudience            = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey         = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
        ),
        ClockSkew                = TimeSpan.Zero
    };
});

// Custom JWT token service (used in AuthController to create tokens)
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

// Build the app with configuration
var app = builder.Build();

// (optional) redirect HTTP to HTTPS in development
// app.UseHttpsRedirection();

// Enable Swagger JSON endpoint
app.UseSwagger();

// Enable Swagger UI
app.UseSwaggerUI();

// Validate JWT tokens on incoming requests
app.UseAuthentication();

// Enforce [Authorize] attributes and policies
app.UseAuthorization();

// Map controller routes (attribute routing)
app.MapControllers();

// Start the application
app.Run();
