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
/// Diagnostic controller that exposes the versions of the loaded Halifax assemblies. Registered automatically
/// by Halifax and accessible anonymously.
/// </summary>
[AllowAnonymous]
public class VersionController : Controller
{
    /// <summary>
    /// Returns the name and version of every currently loaded assembly whose name starts with "Halifax.".
    /// </summary>
    /// <returns>
    /// An <see cref="ApiResponse{T}"/> wrapping the list of <see cref="HalifaxAssemblyInfo"/> for the loaded
    /// Halifax assemblies. Assemblies with a missing name or version report "Unknown" and "0.0.0" respectively.
    /// </returns>
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
