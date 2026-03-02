using Halifax.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Halifax.Api.Controllers;

public record HalifaxAssemblyInfo(string Name, string Version);

[AllowAnonymous]
public class VersionController : Controller
{
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
