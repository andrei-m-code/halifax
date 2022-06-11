using Halifax.Core;
using Halifax.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeggysCove.Api.Filters;

namespace PeggysCove.Api.Controllers;

/// <summary>
/// Lighthouse controller
/// </summary>
[Authorize]
public class LightController : Controller
{
    private readonly AppSettings settings;

    public LightController(AppSettings settings)
    {
        this.settings = settings;
    }
    
    /// <summary>
    /// Get light status
    /// </summary>
    /// <param name="hour">Hour of the day or current UTC time by default</param>
    /// <returns>The value indicating if the light is on or off</returns>
    [HttpGet("status")]
    [AllowAnonymous]
    public ApiResponse<bool> GetStatus([FromQuery] int? hour)
    {
        if (hour.HasValue)
        {
            Guard.Range(hour.Value, nameof(hour), 0, 24);
        }

        hour ??= DateTime.UtcNow.Hour - 4;
        var status = hour is < 6 or > 22;

        return ApiResponse.With(status);
    }

    /// <summary>
    /// Get App Name
    /// </summary>
    [HttpGet("name")]
    public ApiResponse<string> GetName()
    {
        return ApiResponse.With(settings.AppName);
    }

    /// <summary>
    /// Get User token
    /// </summary>
    /// <returns>User token example</returns>
    [HttpGet("token")]
    [AllowAnonymous]
    public ApiResponse<string> GetUserToken()
    {
        return ApiResponse.With(TokenHelper.CreateToken());
    }

    /// <summary>
    /// Check User Authorization
    /// </summary>
    /// <returns>User Authorization</returns>
    [HttpGet("user")]
    [UserAuthorize]
    public ApiResponse<string> AuthorizeUser()
    {
        return ApiResponse.With("User is authorized");
    }
}
