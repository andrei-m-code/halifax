using System;
using Halifax.Core;
using Halifax.Models;
using Microsoft.AspNetCore.Mvc;

namespace PeggysCove.Api.Controllers
{
    /// <summary>
    /// Lighthouse controller
    /// </summary>
    public class LightController : Controller
    {
        /// <summary>
        /// Get light status
        /// </summary>
        /// <param name="hour">Hour of the day or current UTC time by default</param>
        /// <returns>The value indicating if the light is on or off</returns>
        [HttpGet("status")]
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
            return ApiResponse.With(Env.GetSection<AppSettings>().AppName);
        }
    }
}