using LearningResourcesApp.Models.Leermiddel;
using LearningResourcesApp.Client.Services.Interfaces;
using System.Net.Http.Json;

namespace LearningResourcesApp.Client.Services;

public class LeermiddelService : ILeermiddelService
{
    private readonly HttpClient _httpClient;
    private const string ApiBaseUrl = "api/leermiddelen";

    public LeermiddelService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<Leermiddel>> HaalAlleLeermiddelenOp()
    {
        return await VoerGetLijstActieUit(
            async () => await _httpClient.GetFromJsonAsync<List<Leermiddel>>(ApiBaseUrl),
            "ophalen leermiddelen"
        );
    }

    public async Task<Leermiddel?> HaalLeermiddelOpMetId(Guid id)
    {
        return await VoerGetActieUit(
            async () => await _httpClient.GetFromJsonAsync<Leermiddel>($"{ApiBaseUrl}/{id}"),
            "ophalen leermiddel"
        );
    }

    public async Task<bool> VoegLeermiddelToe(Leermiddel leermiddel)
    {
        return await VoerApiActieUit(
            async () => await _httpClient.PostAsJsonAsync(ApiBaseUrl, leermiddel),
            "toevoegen leermiddel"
        );
    }

    public async Task<bool> WijzigLeermiddel(Leermiddel leermiddel)
    {
        return await VoerApiActieUit(
            async () => await _httpClient.PutAsJsonAsync($"{ApiBaseUrl}/{leermiddel.Id}", leermiddel),
            "wijzigen leermiddel"
        );
    }

    public async Task<bool> VoegReactieToe(Guid leermiddelId, Reactie reactie)
    {
        return await VoerApiActieUit(
            async () => await _httpClient.PostAsJsonAsync($"{ApiBaseUrl}/{leermiddelId}/reacties", reactie),
            "toevoegen reactie"
        );
    }

    public async Task<bool> VerwijderLeermiddel(Guid id)
    {
        return await VoerApiActieUit(
            async () => await _httpClient.DeleteAsync($"{ApiBaseUrl}/{id}"),
            "verwijderen leermiddel"
        );
    }

    private async Task<List<T>> VoerGetLijstActieUit<T>(Func<Task<List<T>?>> actie, string actieBeschrijving)
    {
        try
        {
            var resultaat = await actie();
            return resultaat ?? new List<T>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fout bij {actieBeschrijving}: {ex.Message}");
            return new List<T>();
        }
    }

    private async Task<T?> VoerGetActieUit<T>(Func<Task<T?>> actie, string actieBeschrijving)
    {
        try
        {
            return await actie();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fout bij {actieBeschrijving}: {ex.Message}");
            return default;
        }
    }

    private async Task<bool> VoerApiActieUit(Func<Task<HttpResponseMessage>> actie, string actieBeschrijving)
    {
        try
        {
            var response = await actie();
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fout bij {actieBeschrijving}: {ex.Message}");
            return false;
        }
    }
}
