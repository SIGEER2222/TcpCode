using System;
using System.IO;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Serilog;

namespace RxTcpServer
{
    public class ClientHandler
    {
        private readonly TcpClient _client;
        private readonly string _clientEndPoint;

        public ClientHandler(TcpClient client)
        {
            _client = client;
            _clientEndPoint = client.Client.RemoteEndPoint.ToString();
        }

        public async Task HandleClientAsync()
        {
            Log.Information("客户端已连接：{ClientIP}", _clientEndPoint);

            using var stream = _client.GetStream();
            using var reader = new StreamReader(stream);

            var messageObservable = Observable.Create<string>(async observer =>
            {
                try
                {
                    while (_client.Connected)
                    {
                        var line = await reader.ReadLineAsync();
                        if (line == null) break;
                        observer.OnNext(line);
                    }
                    observer.OnCompleted();
                }
                catch (Exception ex)
                {
                    observer.OnError(ex);
                }
            });

            messageObservable
                .Subscribe(async message =>
                {
                    // 获取上次消息
                    string lastMessage = IPTracker.GetLastMessage(_clientEndPoint);

                    if (lastMessage == null || lastMessage != message)
                    {
                        // 更新 IPTracker，缓存新的消息
                        IPTracker.Update(_clientEndPoint, message);

                        // 传递封装的 ClientMessage
                        var clientMessage = new ClientMessage(_clientEndPoint, message, lastMessage);
                       await MessageProcessor.Process(clientMessage);

                        // 记录日志
                        Log.Information("来自 {ClientIP} 的消息：{Message}", _clientEndPoint, message);
                    }
                },
                ex =>
                {
                    Log.Error(ex, "处理客户端 {ClientIP} 消息时发生异常", _clientEndPoint);
                },
                () =>
                {
                    Log.Information("客户端已断开：{ClientIP}", _clientEndPoint);
                    _client.Close();
                    IPTracker.Remove(_clientEndPoint);
                });

            while (_client.Connected)
            {
                await Task.Delay(100);
            }
        }
    }
}
