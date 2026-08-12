using System.Globalization;
using System.Text;
using Halifax.Core.Extensions;
using Microsoft.AspNetCore.Http;

namespace Halifax.Api.Extensions;

/// <summary>
/// Extension methods for <see cref="HttpRequest"/>.
/// </summary>
public static class HttpExtensions
{
    /// <summary>
    /// Reads the HTTP request details into a formatted, human-readable string for logging: the method and
    /// path (including query string), any <c>X-</c> prefixed headers, and the request body.
    /// </summary>
    /// <param name="request">The HTTP request to summarize.</param>
    /// <param name="maxLength">Maximum number of body characters to include; longer bodies are truncated. Defaults to 5000.</param>
    /// <returns>A multi-line string describing the request, suitable for diagnostic logging.</returns>
    /// <remarks>
    /// The request body is only included when its stream is readable; enable buffering (as
    /// <see cref="AppExtensions.UseHalifax"/> does) beforehand so the body can be read here without consuming
    /// it for downstream handlers. The stream position is reset to the start before reading.
    /// </remarks>
    public static async Task<string> GetRequestStringAsync(this HttpRequest request, int maxLength = 5000)
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine("Error happened when processing the request:");

        var path = $"{request.Method}: {request.Path}";
        var body = request.Body;

        if (request.QueryString.HasValue)
        {
            path += request.QueryString.Value;
        }

        stringBuilder.AppendLine(path);

        request.Headers
            .Where(h => h.Key.StartsWith("X-", true, CultureInfo.InvariantCulture))
            .Each(h => stringBuilder.AppendLine($"{h.Key}: {h.Value.ToString()}"));

        if (body.CanRead)
        {
            body.Position = 0;
            using var stream = new StreamReader(request.Body);
            var bodyString = await stream.ReadToEndAsync();
            
            if (!string.IsNullOrWhiteSpace(bodyString))
            {
                if (bodyString.Length > maxLength)
                {
                    bodyString = bodyString[..maxLength];
                }

                stringBuilder.AppendLine($"Body: {bodyString}");
            }
        }

        return stringBuilder.ToString();
    }
}