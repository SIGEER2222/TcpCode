using Serilog;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System;

public class ClientHandler {
  private readonly TcpClient _client;
  private readonly string _clientEndPoint;
  private readonly Subject<string> _messageSubject;

  // 静态字典，用于跟踪13个扫码枪的 Subject
  private static readonly ConcurrentDictionary<string, Subject<string>> ClientSubjects
      = new ConcurrentDictionary<string, Subject<string>>();
  private const int MaxClients = 13; // 限制为13个扫码枪

  public ClientHandler(TcpClient client) {
    _client = client;
    _clientEndPoint = client.Client.RemoteEndPoint.ToString();
    _messageSubject = GetOrCreateSubject(_clientEndPoint);
  }

  private Subject<string> GetOrCreateSubject(string ip) {
    if (ClientSubjects.Count >= MaxClients && !ClientSubjects.ContainsKey(ip)) {
      Log.Warning("已达到最大扫码枪数量（{MaxClients}），拒绝新连接：{IP}", MaxClients, ip);
      _client.Close();
      throw new InvalidOperationException($"已达到最大扫码枪数量限制（{MaxClients}个IP）");
    }

    return ClientSubjects.GetOrAdd(ip, _ =>
    {
      var subject = new Subject<string>();
      SubscribeToSubject(subject, ip); // 为新Subject设置订阅
      return subject;
    });
  }

  private void SubscribeToSubject(Subject<string> subject, string ip) {
    subject
        //.DistinctUntilChanged() // 只关心变化的消息
        .Subscribe(
            message => {
              Log.Information("来自扫码枪 {ClientIP} 的新消息：{Message}", ip, message);
              // 这里可以添加额外的消息处理逻辑，例如：
              // var clientMessage = new ClientMessage(ip, message, IPTracker.GetLastMessage(ip));
              // IPTracker.Update(ip, message);
              // await MessageProcessor.Process(clientMessage);
            },
            ex => Log.Error(ex, "处理扫码枪 {ClientIP} 消息时发生异常", ip),
            () => {
              Log.Information("扫码枪已断开：{ClientIP}", ip);
              if (ClientSubjects.TryRemove(ip, out var removedSubject)) {
                removedSubject.Dispose();
              }
            });
  }

  public async Task HandleClientAsync() {
    Log.Information("扫码枪已连接：{ClientIP}", _clientEndPoint);

    try {
      using var stream = _client.GetStream();
      using var reader = new StreamReader(stream);

      string line;
      while ((line = await reader.ReadLineAsync()) != null) {
        _messageSubject.OnNext(line);
      }
      _messageSubject.OnCompleted();
    }
    catch (Exception ex) {
      Log.Error(ex, "处理扫码枪 {ClientIP} 时发生异常", _clientEndPoint);
      _messageSubject.OnError(ex);
    }
    finally {
      if (_client.Connected) {
        _client.Close();
      }
    }
  }

  public static int GetConnectedClientCount() {
    return ClientSubjects.Count;
  }
}
