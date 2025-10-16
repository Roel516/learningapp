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

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register DateTimeProvider
builder.Services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

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
        policy.Requirements.Add(new InterneMedewerkerRequirement()));
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
})
.AddEntityFrameworkStores<LeermiddelContext>();

// Configure cookie settings for API
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;

    // Return 401 instead of redirect for API calls
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = 401;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = 403;
        return Task.CompletedTask;
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
