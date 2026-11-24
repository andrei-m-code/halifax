using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Halifax.Api.Middleware;
using Halifax.Core.Helpers;
using Halifax.Domain;
using Halifax.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Halifax.Api.Tests;

[ApiController]
public class TestController : ControllerBase
{
    [HttpGet("test/ok")]
    public ApiResponse<string> Hello() => ApiResponse.With("hello");

    [HttpGet("test/not-found")]
    public ApiResponse NotFoundError() => throw new HalifaxNotFoundException("Nothing here");

    [HttpGet("test/bad-request")]
    public ApiResponse BadRequestError() => throw new HalifaxException("Bad input");

    [Authorize]
    [HttpGet("test/secure")]
    public ApiResponse<string> Secure() => ApiResponse.With("secret");
}

/// <summary>
/// Boots a real ASP.NET Core app with AddHalifax/UseHalifax on an in-memory test server and checks the
/// pieces that only fail at runtime: OpenAPI generation (Swashbuckle), the Scalar UI, JWT auth,
/// exception mapping and correlation IDs. Halifax allows one app per process, so all tests share it.
/// </summary>
[TestFixture]
public class HalifaxApiTests
{
    private const string jwtSecret = "halifax-api-tests-secret-that-is-long-enough-for-hs256";

    private WebApplication app = null!;
    private HttpClient client = null!;

    [OneTimeSetUp]
    public async Task StartApp()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddHalifax(halifax => halifax
            .SetName("Halifax Api Tests")
            .ConfigureAuthentication(jwtSecret)
            .ConfigureMvc(mvc => mvc.AddApplicationPart(typeof(TestController).Assembly)));

        app = builder.Build();
        app.UseHalifax();
        await app.StartAsync();
        client = app.GetTestClient();
    }

    [OneTimeTearDown]
    public async Task StopApp()
    {
        client.Dispose();
        await app.StopAsync();
        await app.DisposeAsync();
    }

    [Test]
    public async Task OpenApiDocument_IsGenerated()
    {
        var response = await client.GetAsync("/openapi/v1.json");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;

        Assert.That(root.GetProperty("openapi").GetString(), Does.StartWith("3."));
        Assert.That(root.GetProperty("info").GetProperty("title").GetString(), Is.EqualTo("Halifax Api Tests"));

        var paths = root.GetProperty("paths");
        Assert.That(paths.TryGetProperty("/halifax/version", out _), Is.True);
        Assert.That(paths.TryGetProperty("/test/ok", out _), Is.True);

        var schemes = root.GetProperty("components").GetProperty("securitySchemes");
        Assert.That(schemes.TryGetProperty("Bearer JWT", out _), Is.True);
    }

    [Test]
    public async Task ScalarUi_IsServed()
    {
        var response = await client.GetAsync("/scalar");
        for (var hop = 0; hop < 3 && (int)response.StatusCode is >= 300 and < 400; hop++)
        {
            response = await client.GetAsync(response.Headers.Location);
        }

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("text/html"));
    }

    [Test]
    public async Task VersionEndpoint_ListsHalifaxAssemblies()
    {
        var response = await client.GetAsync("/halifax/version");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var body = await response.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("Halifax.Api"));
        Assert.That(body, Does.Contain("Halifax.Core"));
    }

    [Test]
    public async Task Controller_ReturnsApiResponse()
    {
        var response = await client.GetAsync("/test/ok");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.That(GetProperty(document.RootElement, "success").GetBoolean(), Is.True);
        Assert.That(GetProperty(document.RootElement, "data").GetString(), Is.EqualTo("hello"));
    }

    [TestCase("/test/not-found", HttpStatusCode.NotFound, "HalifaxNotFoundException")]
    [TestCase("/test/bad-request", HttpStatusCode.BadRequest, "HalifaxException")]
    public async Task HalifaxExceptions_MapToStatusCodes(string path, HttpStatusCode expected, string errorType)
    {
        var response = await client.GetAsync(path);

        Assert.That(response.StatusCode, Is.EqualTo(expected));
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.That(GetProperty(document.RootElement, "success").GetBoolean(), Is.False);
        Assert.That(GetProperty(GetProperty(document.RootElement, "error"), "type").GetString(), Is.EqualTo(errorType));
    }

    [Test]
    public async Task SecureEndpoint_WithoutToken_IsUnauthorized()
    {
        var response = await client.GetAsync("/test/secure");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task SecureEndpoint_WithValidToken_IsAllowed()
    {
        var token = Jwt.Create(jwtSecret, [new Claim("sub", "test-user")], DateTime.UtcNow.AddMinutes(5));
        using var request = new HttpRequestMessage(HttpMethod.Get, "/test/secure");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.SendAsync(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task CorrelationId_IsEchoedBack()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/test/ok");
        request.Headers.Add(CorrelationIdMiddleware.HeaderName, "test-correlation-123");

        var response = await client.SendAsync(request);

        Assert.That(response.Headers.GetValues(CorrelationIdMiddleware.HeaderName).Single(),
            Is.EqualTo("test-correlation-123"));
    }

    private static JsonElement GetProperty(JsonElement element, string name)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return property.Value;
            }
        }

        throw new KeyNotFoundException($"Property '{name}' not found in {element}");
    }
}
