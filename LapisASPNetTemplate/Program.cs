using LapisASPNetTemplate.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace LapisASPNetTemplate;

public static class Program
{
    public static string keyId;
    
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.WebHost.UseUrls("http://0.0.0.0:8080");
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddScoped<AuthService>();
        
        // defaultconnection is in appsettings.json
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new Exception("DefaultConnection is not set.");
        }

        Console.WriteLine($"Connection string: {connectionString}");


        builder.Services.AddDbContext<DBContext>(options =>
            options.UseNpgsql(connectionString));

        
        if (builder.Configuration.GetSection("JWTKeyId").Value == null)
        {
            throw new Exception("JWTKeyId is not set in appsettings.json");
        }
            
        keyId = builder.Configuration.GetSection("JWTKeyId").Value;
        
        builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                var rsa = Signatures.GetRsaInstance();
                var securityKey = new RsaSecurityKey(rsa) { KeyId = keyId };

                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = securityKey,
                    ValidateIssuer = false,
                    ValidIssuer = "https://librerate.lapiscreates.tech",
                    ValidateAudience = false,
                    ValidAudience = "https://librerate.lapiscreates.tech",
                    ValidateLifetime = true,
                    RequireExpirationTime = false,
                    ClockSkew = TimeSpan.Zero,
                    NameClaimType = "sub",
                    RoleClaimType = "role"
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var authService = context.HttpContext.RequestServices.GetRequiredService<AuthService>();

                        if (await authService.IsTokenExpired(context.Principal!))
                        {
                            context.Fail("Token has been expired");
                        }
                    }
                };
            });

        builder.Services.AddAuthorization();
        
        WebApplication app = builder.Build();
        app.MapControllers();
        
        // init signatures, if first run it will generate a public and private key into the Data folder.
        // DO NOT SHARE THE PRIVATE KEY FILE UNDER ANY CIRCUMSTANCES!!!
        // public key is fine if its shared, can be used to validate files/other data on the client
        Signatures.Init();
        
        using IServiceScope scope = app.Services.CreateScope();
        DBContext dbContext = scope.ServiceProvider.GetRequiredService<DBContext>();

        try
        {
            dbContext.Database.Migrate();
        }
        catch
        {
            Console.WriteLine($"Failed to migrate database: {dbContext}");
        }
        
        
        await app.RunAsync();
    }
}