using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ---CONFIGURE CORS FOR ANGULAR ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy.WithOrigins("http://localhost:4200") // Trust your local Angular dev server
              .AllowAnyHeader()                     // Allow Authorization tokens
              .AllowAnyMethod();                    // Allow GET, POST, PUT, DELETE
    });
});
// -------------------------------------

// --- 1. CONFIGURE AGGRESSIVE RATE LIMITING ---
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("FixedPolicy", opt =>
    {
        opt.PermitLimit = 20; // ONLY 2 REQUESTS ALLOWED!
        opt.Window = TimeSpan.FromSeconds(15); // PER 15 SECONDS!
        opt.QueueLimit = 0;
    });
});
// ---------------------------------------------

// 1. Grab JWT settings
var secretKey = builder.Configuration.GetValue<string>("JwtOptions:Secret");
var issuer = builder.Configuration.GetValue<string>("JwtOptions:Issuer");
var audience = builder.Configuration.GetValue<string>("JwtOptions:Audience");
var key = Encoding.ASCII.GetBytes(secretKey!);

// 2. Add JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = issuer,
        ValidateAudience = true,
        ValidAudience = audience,
        ValidateLifetime = true, // Rejects expired tokens
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// 3. Register YARP
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseRouting();
app.UseCors("AllowAngularApp");
app.UseRateLimiter();
app.UseAuthentication(); // Must be before Authorization
app.UseAuthorization();  // Must be before MapReverseProxy

app.MapReverseProxy();

app.Run();