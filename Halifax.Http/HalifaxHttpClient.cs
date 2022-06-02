using Halifax.Domain;

namespace Halifax.Http;

public abstract class HalifaxHttpClient
{
    private readonly HttpClient http;

    protected HalifaxHttpClient(HttpClient http)
    {
        this.http = http;
    }

    public Task<ApiResponse<TModel>> SendAsync<TModel>(HttpRequestMessage message)
    {
        return null;
    }
}