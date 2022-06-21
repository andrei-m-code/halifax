using System.Net;
using System.Text;
using Halifax.Core.Helpers;
using Halifax.Domain;
using Halifax.Domain.Exceptions;

namespace Halifax.Http;

public abstract class HalifaxHttpClient
{
    protected readonly HttpClient http;
    
    private static readonly List<HttpStatusCode> exceptionHttpStatuses = new()
    {
        HttpStatusCode.NotFound,
        HttpStatusCode.BadRequest,
        HttpStatusCode.Unauthorized
    };

    protected HalifaxHttpClient(HttpClient http)
    {
        this.http = http;
    }

    protected virtual HttpRequestMessage CreateMessage(HttpMethod method, string url, object body = default)
    {
        var message = new HttpRequestMessage(method, url);

        if (body != null)
        {
            var json = Json.Serialize(body);
            message.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        return message;
    }
    
    protected virtual async Task<TModel> SendAsync<TModel>(
        HttpRequestMessage message, 
        CancellationToken cancellationToken = default)
    {
        var (responseString, _) = await SendAsync(message, cancellationToken);

        var result = DeserializeApiResponseOrThrow<TModel>(responseString);

        return result;
    }

    protected virtual async Task<(string responseString, HttpStatusCode statusCode)> SendAsync(
        HttpRequestMessage message, 
        CancellationToken cancellationToken = default)
    {
        using var response = await http.SendAsync(message, cancellationToken);
        var responseString = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw MapToException(response, responseString);
        }
        
        return (responseString, response.StatusCode);
    }
    
    protected virtual Exception MapToException(HttpResponseMessage response, string content)
    {
        var code = response.StatusCode;
        
        if (exceptionHttpStatuses.Contains(code))
        {
            var model = Json.Deserialize<ApiResponse>(content);

            if (string.IsNullOrWhiteSpace(model.Error?.Message))
            {
                return ErrorReadingTheResponse();
            }
            
            switch (code)
            {
                case HttpStatusCode.BadRequest:
                    return new HalifaxException(model.Error.Message);
                
                case HttpStatusCode.NotFound:
                    return new HalifaxNotFoundException(model.Error.Message);
                
                case HttpStatusCode.Unauthorized:
                    return new HalifaxUnauthorizedException(model.Error.Message);
            }
        }
        
        return new Exception($"Unsuccessful request. {GetType().Name} HTTP {code}");
    }

    protected virtual TModel DeserializeApiResponseOrThrow<TModel>(string content)
    {
        try
        {
            var deserialized = Json.Deserialize<ApiResponse<TModel>>(content);
            return deserialized.Data;
        }
        catch
        {
            throw ErrorReadingTheResponse();
        }
    }

    private Exception ErrorReadingTheResponse()
    {
        return new Exception($"Error reading the response of the {GetType().Name}");
    }
}