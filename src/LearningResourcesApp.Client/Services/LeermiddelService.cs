using LearningResourcesApp.Client.Models.Leermiddelen;
using System.Net.Http.Json;

namespace LearningResourcesApp.Client.Services;

public class LeermiddelService
{
    private readonly HttpClient _httpClient;
    private const string ApiBaseUrl = "api/leermiddelen";

    public LeermiddelService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<Leermiddel>> HaalAlleLeermiddelenOp()
    {
        try
        {
            var leermiddelen = await _httpClient.GetFromJsonAsync<List<Leermiddel>>(ApiBaseUrl);
            return leermiddelen ?? new List<Leermiddel>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fout bij ophalen leermiddelen: {ex.Message}");
            return new List<Leermiddel>();
        }
    }

    public async Task<Leermiddel?> HaalLeermiddelOpMetId(Guid id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<Leermiddel>($"{ApiBaseUrl}/{id}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fout bij ophalen leermiddel: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> VoegLeermiddelToe(Leermiddel leermiddel)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(ApiBaseUrl, leermiddel);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fout bij toevoegen leermiddel: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> WijzigLeermiddel(Leermiddel leermiddel)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"{ApiBaseUrl}/{leermiddel.Id}", leermiddel);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fout bij wijzigen leermiddel: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> VoegReactieToe(Guid leermiddelId, Reactie reactie)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"{ApiBaseUrl}/{leermiddelId}/reacties", reactie);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fout bij toevoegen reactie: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> VerwijderLeermiddel(Guid id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"{ApiBaseUrl}/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fout bij verwijderen leermiddel: {ex.Message}");
            return false;
        }
    }
}
