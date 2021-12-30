using Halifax.Domain;

namespace Halifax.HttpClient;

public abstract class HalifaxHttpClient
{
    private readonly HttpClient http;

    public HalifaxHttpClient(HttpClient http)
    {
        this.http = http;
    }

    public Task<ApiResponse<TModel>> SendAsync<TModel>(HttpRequestMessage message)
    {
        
        return null;
    }
}