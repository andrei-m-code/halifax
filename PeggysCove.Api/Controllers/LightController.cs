using System;
using Halifax.Api.Models;
using Halifax.Core;
using Microsoft.AspNetCore.Mvc;

namespace PeggysCove.Api.Controllers
{
    public class LightController : Controller
    {
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
    }
}