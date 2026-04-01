using Halifax.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Halifax.Api.Controllers;

/// <summary>
/// Assembly version information for a Halifax package.
/// </summary>
/// <param name="Name">The assembly name.</param>
/// <param name="Version">The assembly version.</param>
public record HalifaxAssemblyInfo(string Name, string Version);

/// <summary>
/// Exposes Halifax assembly version information.
/// </summary>
[AllowAnonymous]
public class VersionController : Controller
{
    /// <summary>
    /// Returns the version of all loaded Halifax assemblies.
    /// </summary>
    [HttpGet("halifax/version")]
    public ApiResponse<List<HalifaxAssemblyInfo>> GetHalifaxVersion()
    {
        var assemblies = AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(a => a.GetName().Name?.StartsWith("Halifax.") == true)
            .Select(a =>
            {
                var name = a.GetName();
                return new HalifaxAssemblyInfo(
                    name.Name ?? "Unknown",
                    name.Version?.ToString() ?? "0.0.0");
            })
            .ToList();

        return ApiResponse.With(assemblies);
    }
}
