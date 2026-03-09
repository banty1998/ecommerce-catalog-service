using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
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


// 4. THESE MUST BE IN THIS EXACT ORDER
app.UseRouting();
app.UseAuthentication(); // Must be before Authorization
app.UseAuthorization();  // Must be before MapReverseProxy

app.MapReverseProxy();

app.Run();