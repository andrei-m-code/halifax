using System;
using System.Net;
using System.Threading.Tasks;

namespace Halifax.Api.Errors
{
    public interface IExceptionHandler
    {
        Task<(object Response, HttpStatusCode Code)> HandleAsync(Exception exception);
    }
}