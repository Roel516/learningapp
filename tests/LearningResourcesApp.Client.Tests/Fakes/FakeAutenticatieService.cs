using LearningResourcesApp.Client.Models.Authenticatie;
using LearningResourcesApp.Client.Services;
using Microsoft.JSInterop;

namespace LearningResourcesApp.Client.Tests.Fakes;

public class FakeAutenticatieService : AutenticatieService
{
    private Gebruiker? _gebruiker;

    public FakeAutenticatieService() : base(new HttpClient(), new FakeJSRuntime())
    {
    }

    public void SetHuidigeGebruiker(Gebruiker? gebruiker)
    {
        _gebruiker = gebruiker;
        // Use reflection to set the private field
        var field = typeof(AutenticatieService).GetField("_huidigeGebruiker",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(this, gebruiker);
    }

    public new Gebruiker? HuidigeGebruiker => _gebruiker;
    public new bool IsIngelogd => _gebruiker?.IsIngelogd ?? false;
}

public class FakeJSRuntime : IJSRuntime
{
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
    {
        return ValueTask.FromResult<TValue>(default!);
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        return ValueTask.FromResult<TValue>(default!);
    }
}
