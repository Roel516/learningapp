using LearningResourcesApp.Authorization;
using LearningResourcesApp.Data;
using LearningResourcesApp.Models.Leermiddel;
using LearningResourcesApp.Models.Auth;
using LearningResourcesApp.Repositories;
using LearningResourcesApp.Repositories.Interfaces;
using LearningResourcesApp.Services;
using LearningResourcesApp.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Learning Resources API",
        Version = "v1",
        Description = "API voor beheer van leermiddelen met JWT authenticatie ondersteuning"
    });

    // Voeg JWT authenticatie toe aan Swagger
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Voorbeeld: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
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

// Register DateTimeProvider
builder.Services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

// Register JWT Token Service
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

// Register Repositories
builder.Services.AddScoped<ILeermiddelRepository, LeermiddelRepository>();
builder.Services.AddScoped<IReactieRepository, ReactieRepository>();

// Register Helpers
builder.Services.AddScoped<LearningResourcesApp.Helpers.ControllerExceptionHandler>();
builder.Services.AddScoped<LearningResourcesApp.Helpers.ExceptionHandler>();

// Register Authorization Handler
builder.Services.AddSingleton<IAuthorizationHandler, InterneMedewerkerHandler>();

// Configure Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.InterneMedewerker, policy =>
    {
        // Support both Cookie and JWT authentication
        policy.AuthenticationSchemes.Add(IdentityConstants.ApplicationScheme);
        policy.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);
        policy.Requirements.Add(new InterneMedewerkerRequirement());
    });

    // Default policy for [Authorize] attribute - supports both schemes
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, JwtBearerDefaults.AuthenticationScheme)
        .RequireAuthenticatedUser()
        .Build();
});

// Configure DbContext - SQL Server for both local (LocalDB) and Azure
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<LeermiddelContext>(options =>
    options.UseSqlServer(connectionString));

var isLocalDb = connectionString?.Contains("localdb", StringComparison.OrdinalIgnoreCase) == true;
var isAzure = connectionString?.Contains("database.windows.net", StringComparison.OrdinalIgnoreCase) == true;
Console.WriteLine($"Using SQL Server: {(isLocalDb ? "LocalDB (Development)" : isAzure ? "Azure SQL Database (Production)" : "SQL Server")}");

// Configure Identity
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;

    // User settings
    options.User.RequireUniqueEmail = true;
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+ ";
})
.AddEntityFrameworkStores<LeermiddelContext>();

// Configure cookie settings for API (for Blazor WebAssembly client)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
});

// Configure JWT Authentication (for external API consumers)
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

builder.Services.AddAuthentication(options =>
{
    // Default scheme remains cookie for the Blazor app
    options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
    options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
        ClockSkew = TimeSpan.Zero // Remove default 5-minute clock skew for more precise expiration
    };

    // Add event handlers for debugging
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"JWT Authentication failed: {context.Exception.Message}");
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Console.WriteLine($"JWT Token validated successfully for user: {context.Principal?.Identity?.Name}");
            return Task.CompletedTask;
        },
        OnMessageReceived = context =>
        {
            Console.WriteLine($"JWT Token received: {context.Token?.Substring(0, Math.Min(20, context.Token?.Length ?? 0))}...");
            return Task.CompletedTask;
        }
    };
});

var app = builder.Build();

// Apply pending migrations and seed data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<LeermiddelContext>();

    // Pas migraties automatisch toe (werkt voor zowel LocalDB als Azure SQL)
    Console.WriteLine("Applying database migrations...");
    context.Database.Migrate();
    Console.WriteLine("Database migrations applied successfully.");

    // Seed admin user
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    await SeedAdminUser(userManager);   
    await SeedLeermiddelen(context, userManager);    
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseWebAssemblyDebugging();
}

app.UseHttpsRedirection();

// Serve static files from the Blazor WebAssembly app
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Fallback to index.html for client-side routing
app.MapFallbackToFile("index.html");

app.Run();

static async Task SeedAdminUser(UserManager<IdentityUser> userManager)
{
    const string adminEmail = "admin@admin.nl";
    const string adminPassword = "admin123";
    const string adminName = "Administrator";

    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        adminUser = new IdentityUser
		{
            UserName = adminName,
            Email = adminEmail,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(adminUser, adminPassword);
        if (result.Succeeded)
        {
            // Voeg InterneMedewerker claim toe aan admin
            await userManager.AddClaimAsync(adminUser, new Claim(AppClaims.InterneMedewerker, "true"));
            Console.WriteLine($"Admin user created: {adminEmail}");
        }
        else
        {
            Console.WriteLine($"Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
    }
    else
    {
        // Zorg ervoor dat bestaande admin user de claim heeft
        var claims = await userManager.GetClaimsAsync(adminUser);
        if (!claims.Any(c => c.Type == AppClaims.InterneMedewerker))
        {
            await userManager.AddClaimAsync(adminUser, new Claim(AppClaims.InterneMedewerker, "true"));
            Console.WriteLine($"InterneMedewerker claim toegevoegd aan bestaande admin gebruiker: {adminEmail}");
        }
    }
}

static async Task SeedLeermiddelen(LeermiddelContext context, UserManager<IdentityUser> userManager)
{
    // Check of er al leermiddelen zijn
    if (context.Leermiddelen.Any())
    {
        return; // Database heeft al data
    }

    var leermiddelen = new[]
    {
        new Leermiddel
        {
            Titel = "C# Programming Guide",
            Beschrijving = "Een uitgebreide gids voor C# programmeren",
            Link = "https://learn.microsoft.com/en-us/dotnet/csharp/",
            AangemaaktOp = DateTime.UtcNow
        },
        new Leermiddel
        {
            Titel = "ASP.NET Core Documentation",
            Beschrijving = "Complete documentatie voor ASP.NET Core web development",
            Link = "https://learn.microsoft.com/en-us/aspnet/core/",
            AangemaaktOp = DateTime.UtcNow
        },
        new Leermiddel
        {
            Titel = "Blazor Tutorial",
            Beschrijving = "Leer Blazor WebAssembly en Server vanaf nul",
            Link = "https://dotnet.microsoft.com/learn/aspnet/blazor-tutorial/intro",
            AangemaaktOp = DateTime.UtcNow
        },
        new Leermiddel
        {
            Titel = "Entity Framework Core",
            Beschrijving = "Database toegang met Entity Framework Core ORM",
            Link = "https://learn.microsoft.com/en-us/ef/core/",
            AangemaaktOp = DateTime.UtcNow
        },
        new Leermiddel
        {
            Titel = "Git Version Control",
            Beschrijving = "Basis en gevorderde Git concepten voor versiebeheer",
            Link = "https://git-scm.com/doc",
            AangemaaktOp = DateTime.UtcNow
        }
    };

    context.Leermiddelen.AddRange(leermiddelen);
    await context.SaveChangesAsync();
    Console.WriteLine($"{leermiddelen.Length} test leermiddelen toegevoegd aan database");
}
