using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Serilog;

namespace RxTcpServer {
  public class TcpServer {
    private readonly int _port;

    public TcpServer(int port) {
      _port = port;
    }

    public async Task StartAsync() {
      var listener = new TcpListener(IPAddress.Any, _port);
      listener.Start();
      Log.Information("服务器已启动，监听端口 {Port}", _port);

      while (true) {
        var client = await listener.AcceptTcpClientAsync();
        _ = Task.Run(() => new ClientHandler(client).HandleClientAsync());
      }
    }
  }
}
