using System.Net;
using Halifax.Core.Helpers;
using Halifax.Domain;
using Halifax.Domain.Exceptions;

namespace Halifax.Http;

public abstract class HalifaxHttpClient
{
    private readonly HttpClient http;
    
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

    protected virtual async Task<ApiResponse<TModel>> SendAsync<TModel>(
        HttpRequestMessage message, 
        CancellationToken cancellationToken = default)
    {
        using var response = await http.SendAsync(message, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        
        if (response.IsSuccessStatusCode)
        {
            var deserialized = DeserializeApiResponseOrThrow<TModel>(content);
            return deserialized;
        }

        throw MapToException(response, content);
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

    protected virtual ApiResponse<TModel> DeserializeApiResponseOrThrow<TModel>(string content)
    {
        try
        {
            var deserialized = Json.Deserialize<ApiResponse<TModel>>(content);
            return deserialized;
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