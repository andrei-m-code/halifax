using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using PeggysCove.Api;

Host.CreateDefaultBuilder(args)
    .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>())
    .Build().Run();
