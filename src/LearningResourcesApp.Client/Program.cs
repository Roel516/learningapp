using LearningResourcesApp.Client;
using LearningResourcesApp.Client.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Registreer services
builder.Services.AddScoped<LeermiddelService>();
builder.Services.AddScoped<AutenticatieService>();

var host = builder.Build();

// Initialiseer authenticatie service
var autenticatieService = host.Services.GetRequiredService<AutenticatieService>();
await autenticatieService.Initialiseer();

await host.RunAsync();
