using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;
using TTAPI;
using TTAPI.Recv;
using TTAPI.Send;

namespace Hashtag_Amy
{
    class Program
    {
        private static TTClient client;

        static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.File($"logs/amy.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            ServiceCollection services = new ServiceCollection();

            services.AddLogging(logs => logs.AddSerilog());
            services.AddScoped<TTClient>();

            string userId = Environment.GetEnvironmentVariable("TT_USERID");
            string authId = Environment.GetEnvironmentVariable("TT_AUTHID");
            string roomId = Environment.GetEnvironmentVariable("TT_ROOMID");

            var provider = services.BuildServiceProvider();

            CancellationTokenSource source = new CancellationTokenSource();
            try
            {
                client = provider.GetRequiredService<TTClient>();

                while (!client.IsConnected)
                {
                    var results = await client.ConnectAsync(userId, authId, roomId, source.Token);
                    await Task.Delay(3000);
                }

                await Task.Delay(-1);
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "???");
                return;
            }
        }
    }
}
