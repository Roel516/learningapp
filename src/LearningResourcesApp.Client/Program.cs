using LearningResourcesApp.Client;
using LearningResourcesApp.Client.Services;
using LearningResourcesApp.Client.Services.Interfaces;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Registreer services met interfaces
builder.Services.AddScoped<ILeermiddelService, LeermiddelService>();
builder.Services.AddScoped<IAutenticatieService, AutenticatieService>();

var host = builder.Build();

// Initialiseer authenticatie service
var autenticatieService = host.Services.GetRequiredService<IAutenticatieService>();
await autenticatieService.Initialiseer();

await host.RunAsync();
