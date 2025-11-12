using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using MARS_BE.Infrastructure.Auth;

// Starts the configuration of the app
var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// For Swagger
builder.Services.AddEndpointsApiExplorer();

// Swagger UI for documentation
builder.Services.AddSwaggerGen();

// JWT Config
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()!;

// Authentication
builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(o =>
    {   // Token validation rules
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, // verifies the source
            ValidateAudience = true, // verifies the source
            ValidateIssuerSigningKey = true, // verifies if correctly signed
            ValidateLifetime = true, // verifies if not expired
            ValidIssuer = jwt.Issuer, 
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)), // secret key
            ClockSkew = TimeSpan.Zero // no extra time margin
        };
    });

// Inject/adds the custom JWT service
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

// Build the app with configuration
var app = builder.Build();

// Middleware pipeline 

// Activates swagger JSON endpoint
app.UseSwagger();

// Activates swagger UI
app.UseSwaggerUI();

// Checks JWT tokens
app.UseAuthentication();

// Checks [Autorize] attributes
app.UseAuthorization();

// Routes request to controllers
app.MapControllers();

// Starts the app
app.Run();