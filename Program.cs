using System;
using System.Threading.Tasks;
using Serilog;

namespace RxTcpServer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File("logs/TcpServerLog.log")
                .WriteTo.Debug()
                .CreateLogger();

            var server = new TcpServer(8080);
            await server.StartAsync();
        }
    }
}
