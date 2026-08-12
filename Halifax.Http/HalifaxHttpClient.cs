using System.Net;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using Halifax.Core.Helpers;
using Halifax.Domain;
using Halifax.Domain.Exceptions;

namespace Halifax.Http;

/// <summary>
/// Base class for typed HTTP clients that communicate with Halifax-style APIs.
/// Handles JSON serialization, response parsing from the <see cref="ApiResponse{TData}"/> envelope,
/// and mapping of downstream error responses to Halifax domain exceptions.
/// </summary>
/// <remarks>
/// Derive from this class to build a strongly typed client for a specific API. Register the derived
/// type with one of the <c>AddHalifaxHttpClient</c> extensions so that its <see cref="HttpClient"/>
/// dependency is supplied by the framework's <c>IHttpClientFactory</c>. Use the protected
/// <see cref="CreateMessage"/> helper to build requests and the protected <see cref="SendAsync{TModel}(HttpRequestMessage, CancellationToken)"/>
/// overloads to send them and unwrap the response.
/// </remarks>
/// <example>
/// <code>
/// public class UsersClient(HttpClient http) : HalifaxHttpClient(http)
/// {
///     public Task&lt;User&gt; GetUserAsync(int id, CancellationToken cancellationToken = default)
///     {
///         var message = CreateMessage(HttpMethod.Get, $"users/{id}");
///         return SendAsync&lt;User&gt;(message, cancellationToken);
///     }
/// }
/// </code>
/// </example>
public abstract class HalifaxHttpClient(HttpClient http)
{
    /// <summary>The underlying <see cref="HttpClient"/> instance used to send requests.</summary>
    protected readonly HttpClient http = http;
    
    private static readonly List<HttpStatusCode> exceptionHttpStatuses =
    [
        HttpStatusCode.NotFound,
        HttpStatusCode.BadRequest,
        HttpStatusCode.Unauthorized
    ];

    /// <summary>
    /// Creates an <see cref="HttpRequestMessage"/> for the given method and URL, optionally
    /// serializing <paramref name="body"/> as a UTF-8 JSON request content.
    /// </summary>
    /// <param name="method">The HTTP method (verb) for the request.</param>
    /// <param name="url">
    /// The request URL, relative to the client's configured base address or absolute.
    /// </param>
    /// <param name="body">
    /// The object to serialize as the JSON request body, or <see langword="null"/> for a request
    /// without a body.
    /// </param>
    /// <returns>
    /// A new <see cref="HttpRequestMessage"/> with JSON content attached when <paramref name="body"/>
    /// is supplied.
    /// </returns>
    /// <remarks>
    /// Serialization uses the shared Halifax JSON settings. Override this method in a derived class
    /// to customize how request messages are built (for example, to add per-request headers).
    /// </remarks>
    protected virtual HttpRequestMessage CreateMessage(HttpMethod method, string url, object? body = null)
    {
        var message = new HttpRequestMessage(method, url);

        if (body != null)
        {
            var json = Json.Serialize(body)!;
            message.Content = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);
        }

        return message;
    }

    /// <summary>
    /// Sends the given request and returns the resulting HTTP status code without reading the response body.
    /// </summary>
    /// <param name="message">The request message to send.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The <see cref="HttpStatusCode"/> returned by the downstream service.</returns>
    /// <remarks>
    /// Unlike the generic <see cref="SendAsync{TModel}(HttpRequestMessage, CancellationToken)"/> overload,
    /// this method does not inspect the status code or map errors to exceptions; the raw status code is
    /// returned to the caller for inspection. Intended for use and overriding by derived clients.
    /// </remarks>
    protected virtual async Task<HttpStatusCode> SendAsync(HttpRequestMessage message, CancellationToken cancellationToken = default)
    {
        using var response = await http.SendAsync(message, cancellationToken);
        return response.StatusCode;
    }
    
    /// <summary>
    /// Sends the given request and deserializes the <see cref="ApiResponse{TData}.Data"/> payload
    /// from the <see cref="ApiResponse{TData}"/> envelope returned by a Halifax-style API.
    /// </summary>
    /// <typeparam name="TModel">The type of the data payload to deserialize from the response envelope.</typeparam>
    /// <param name="message">The request message to send.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The deserialized <typeparamref name="TModel"/> data from a successful response.</returns>
    /// <remarks>
    /// On a successful response the JSON body is deserialized into <see cref="ApiResponse{TData}"/> and
    /// its <see cref="ApiResponse{TData}.Data"/> is returned. Unsuccessful responses are routed through
    /// <see cref="HandleUnsuccessfulResponseAsync"/>, which maps recognized status codes to Halifax
    /// domain exceptions. Intended for use and overriding by derived clients.
    /// </remarks>
    /// <exception cref="HalifaxException">The downstream service returned HTTP 400 (Bad Request).</exception>
    /// <exception cref="HalifaxNotFoundException">The downstream service returned HTTP 404 (Not Found).</exception>
    /// <exception cref="HalifaxUnauthorizedException">The downstream service returned HTTP 401 (Unauthorized).</exception>
    /// <exception cref="Exception">
    /// The response body could not be read or parsed, or the request failed with a status code other than
    /// 400, 401, or 404.
    /// </exception>
    protected virtual async Task<TModel> SendAsync<TModel>(
        HttpRequestMessage message,
        CancellationToken cancellationToken = default)
    {
        var opts = new JsonSerializerOptions();
        Json.ConfigureOptions(opts);
        var response = await SendInternalAsync<TModel>(message, opts, cancellationToken);
        return response.Data;
    }

    private async Task<ApiResponse<TModel>> SendInternalAsync<TModel>(
        HttpRequestMessage message, 
        JsonSerializerOptions jsonSerializerOptions,
        CancellationToken cancellationToken = default) 
    {
        using var response = await http.SendAsync(message, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            try
            {
                var model = await response.Content.ReadFromJsonAsync<ApiResponse<TModel>>(
                    jsonSerializerOptions,
                    cancellationToken);
                
                if (model != null)
                {
                    return model;
                }
            }
            catch (Exception ex)
            {
                L.Error($"Error parsing the response. {message.RequestUri}", ex);
                throw ErrorReadingTheResponse();
            }
        }

        await HandleUnsuccessfulResponseAsync(response);
        
        // exception is thrown in most cases
        return null!;
    }
    
    /// <summary>
    /// Handles an unsuccessful HTTP response by reading the <see cref="ApiResponse"/> error payload and
    /// mapping the status code to the corresponding Halifax domain exception.
    /// </summary>
    /// <param name="response">The unsuccessful HTTP response to inspect.</param>
    /// <returns>A task that always completes by throwing; it never returns normally.</returns>
    /// <remarks>
    /// Status codes are mapped as follows: HTTP 400 to <see cref="HalifaxException"/>, HTTP 404 to
    /// <see cref="HalifaxNotFoundException"/>, and HTTP 401 to <see cref="HalifaxUnauthorizedException"/>,
    /// each carrying the error message from the response envelope. Any other status code, or a response
    /// whose body cannot be parsed into an error envelope, results in a generic <see cref="Exception"/>.
    /// Override this method to customize error handling in a derived client.
    /// </remarks>
    /// <exception cref="HalifaxException">The response status code is HTTP 400 (Bad Request).</exception>
    /// <exception cref="HalifaxNotFoundException">The response status code is HTTP 404 (Not Found).</exception>
    /// <exception cref="HalifaxUnauthorizedException">The response status code is HTTP 401 (Unauthorized).</exception>
    /// <exception cref="Exception">
    /// The error envelope could not be read or parsed, or the status code is not one of the mapped values.
    /// </exception>
    protected virtual async Task HandleUnsuccessfulResponseAsync(HttpResponseMessage response)
    {
        var code = response.StatusCode;
        var content = await response.Content.ReadAsStringAsync();
        
        L.Warning($"{GetType().Name}: Request error. {code}\r\n{content}");
        
        if (exceptionHttpStatuses.Contains(code))
        {
            ApiResponse? model;

            try
            {
                model = Json.Deserialize<ApiResponse>(content);
                
                if (string.IsNullOrWhiteSpace(model?.Error?.Message))
                {
                    L.Warning("Response model doesn't have error information");
                    throw ErrorReadingTheResponse();
                }
            }
            catch (Exception ex)
            {
                L.Error($"Error parsing the response. {response.RequestMessage?.RequestUri}", ex);
                throw ErrorReadingTheResponse();
            }
            
            switch (code)
            {
                case HttpStatusCode.BadRequest:
                    throw new HalifaxException(model.Error.Message);
                
                case HttpStatusCode.NotFound:
                    throw new HalifaxNotFoundException(model.Error.Message);
                
                case HttpStatusCode.Unauthorized:
                    throw new HalifaxUnauthorizedException(model.Error.Message);
            }
        }
        
        throw new Exception($"Unsuccessful request. {GetType().Name}. HTTP {code}");
    }

    private Exception ErrorReadingTheResponse()
    {
        return new Exception($"Error reading the response of the {GetType().Name}");
    }
}