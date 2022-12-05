using System.Globalization;
using System.Text;
using Halifax.Core.Extensions;
using Microsoft.AspNetCore.Http;

namespace Halifax.Api.Extensions;

public static class HttpExtensions
{
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