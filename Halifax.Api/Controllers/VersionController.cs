using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Halifax.Api.Controllers;

[AllowAnonymous]
public class VersionController
{
    [HttpGet("halifax/version")]
    public List<string> GetHalifaxVersion()
    {
        var assemblies = AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(a => a.FullName.StartsWith("Halifax."))
            .Select(a => a.FullName)
            .ToList();


        return assemblies;
    }
}
