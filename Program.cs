using Serilog;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;

class Program {
  static void Main() {
    Log.Logger = new LoggerConfiguration()
        .WriteTo.Console()
        .WriteTo.File("logs/TcpServerLog.log")
        .WriteTo.Debug()
        .CreateLogger();

    var db = SqlSugarHelp.Db;
    db.Queryable<BarcodeScannerLog>().SplitTable().ToList();

    var messageProcessor = new MessageProcessor();
    messageProcessor.StartProcessing();

    var tcpServer = new TcpServer(messageProcessor);
    tcpServer.Start();

    Console.ReadLine(); // Keep the server running
  }
}

public class TcpServer {
  private readonly TcpListener tcpListener;
  private readonly MessageProcessor messageProcessor;

  public TcpServer(MessageProcessor messageProcessor) {
    this.messageProcessor = messageProcessor;
    this.tcpListener = new TcpListener(IPAddress.Any, 8080);
  }

  public void Start() {
    tcpListener.Start();
    Log.Information("TCP Server started on port 8080.");
    AcceptClientsAsync();
  }

  private async void AcceptClientsAsync() {
    while (true) {
      var tcpClient = await tcpListener.AcceptTcpClientAsync();
      Log.Information("Client connected: {0}", tcpClient.Client.RemoteEndPoint);

      // Process the client connection in a separate task
      _ = Task.Run(() => HandleClientAsync(tcpClient));
    }
  }

  private async Task HandleClientAsync(TcpClient tcpClient) {
    using (tcpClient) {
      var stream = tcpClient.GetStream();
      var buffer = new byte[1024];
      int bytesRead;

      while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0) {
        var message = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
        var clientId = tcpClient.Client.RemoteEndPoint.ToString(); // Use remote IP as clientId
        Log.Information("Received message from {0}: {1}", clientId, message);

        // Pass the message to the message processor
        messageProcessor.ReceiveMessage(clientId, message);
      }
    }
  }
}

public class MessageProcessor {
  private static readonly ReplaySubject<(string clientId, string message)> messageStream = new(1);
  private readonly IClientJobService clientJobService;

  public MessageProcessor() {
    clientJobService = new ClientJobService();
  }

  public void StartProcessing() {
    messageStream
        .Where(data => !string.IsNullOrEmpty(data.clientId)
        && !string.IsNullOrEmpty(data.message)
        && DataProcessor.IsSpecificTextFormat(data.message))
         .SelectMany(data =>
        {
            var messages = data.message.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            return messages.Select(message => (data.clientId, message.Trim()));
        })
        .GroupBy(data => data.clientId)
        .Catch<IGroupedObservable<string, (string clientId, string message)>, Exception>(ex => {
          Log.Error($"[Fatal Error] GroupBy 发生异常: {ex.Message}");
          return Observable.Empty<IGroupedObservable<string, (string clientId, string message)>>();
        })
        .Subscribe(
            group => {
              group
                .DistinctUntilChanged(data => data.message)
                .ObserveOn(TaskPoolScheduler.Default)
                .Subscribe(async data => await ProcessClientMessage(group.Key, data.clientId.Split(":")?.First(), data.message),
                    ex => Log.Error($"[Error] 处理客户端 {group.Key} 失败: {ex.Message}")
                );
            },
            ex => Log.Error($"[Fatal Error] 发生未处理的异常: {ex.Message}")
        );
  }

  // This method is called when a new message is received from a client
  public void ReceiveMessage(string clientId, string message) {
    messageStream.OnNext((clientId, message));
  }

  // 异步处理客户端消息
  private async Task ProcessClientMessage(string groupKey, string clientId, string code) {
    // 获取客户端任务
    var clientJob = clientJobService.GetClientJob(clientId);
    // 如果没有找到匹配的客户端配置，则记录错误日志并返回
    if (clientJob == null) {
      Log.Error("未找到匹配的客户端配置：[IP: {IP}] [当前消息: {Message}]", clientId, code);
      return;
    }

    // 获取数据库连接
    var db = SqlSugarHelp.Db;
    // 创建条码扫描日志对象
    var barCodeLog = new BarcodeScannerLog() {
      IP = clientId,
      Code = code,
    };

    if (DataProcessor.IsSpecificTextFormat(code)) {
      var carrierName = code;
      foreach (var job in clientJob) {
        barCodeLog.LogTime = DateTime.Now;
        if (job.Type == JobType.In) {
          Log.Information($"处理消息: [IP: {clientId}] [当前消息: {code}] 执行动作: [In] ");
          var massage = await EapJobService.SendJobInRequestAsync(job.Code, carrierName);
          barCodeLog.JobType = JobType.In;
          barCodeLog.Message = massage;
        }
        else if (job.Type == JobType.Out) {
          var massage = await EapJobService.SendJobOutRequestAsync(job.Code, carrierName);
          barCodeLog.JobType = JobType.Out;
          barCodeLog.Message = massage;
          Log.Information($"处理消息: [IP: {clientId}] [当前消息: {code}] 执行动作: [Out] ");
        }
        barCodeLog.EqpName = job.Code;
        await db.Insertable(barCodeLog).SplitTable()
          .ExecuteReturnSnowflakeIdListAsync();
      }
    }
    // 如果消息格式不符合特定格式，则记录错误日志
    else {
      Log.Error("处理消息: [IP: {IP}] [当前消息: {Message}]", clientId, code);
    }
  }
}

public interface IClientJobService {
  List<ClientJob> GetClientJob(string clientId);
}

public class ClientJobService : IClientJobService {
  private static readonly Dictionary<string, List<ClientJob>> clientJobMappings = new()
   {
        { "10.10.0.101", new List<ClientJob> { new ClientJob("MCT-SC-A-0038", JobType.In) } },
        { "10.10.0.102", new List<ClientJob> { new ClientJob("MCT-SC-A-0038", JobType.Out) } },
        { "10.10.0.103", new List<ClientJob> { new ClientJob("MCT-SC-A-0039", JobType.In) } },
        { "10.10.0.104", new List<ClientJob> { new ClientJob("MCT-SC-A-0039", JobType.Out),
          new ClientJob("MCT-SC-A-0040", JobType.In) } },
        { "10.10.0.105", new List<ClientJob> { new ClientJob("MCT-SC-A-0040", JobType.Out),
          new ClientJob("MCT-SC-A-0041", JobType.In) } },
        { "10.10.0.106", new List<ClientJob> { new ClientJob("MCT-SC-A-0041", JobType.Out),
          new ClientJob("MCT-SC-A-0042", JobType.In) } },
        { "10.10.0.108", new List<ClientJob> { new ClientJob("MCT-SC-A-0042", JobType.Out) } },
        { "10.10.0.109", new List<ClientJob> { new ClientJob("MCT-SC-A-0043", JobType.In) } },
        { "10.10.0.110", new List<ClientJob> { new ClientJob("MCT-SC-A-0043", JobType.Out),
          new ClientJob("MCT-SC-A-0044", JobType.In) } },
        { "10.10.0.111", new List<ClientJob> { new ClientJob("MCT-SC-A-0044", JobType.Out) } },
        { "10.10.0.112", new List<ClientJob> { new ClientJob("MCT-SC-A-0045", JobType.In) } },
        { "10.10.0.107", new List<ClientJob> { new ClientJob("MCT-SC-A-0045", JobType.Out),
          new ClientJob("MCT-SC-A-0046", JobType.In) } },
        { "10.10.0.113", new List<ClientJob> { new ClientJob("MCT-SC-A-0046", JobType.Out) } }
    };

  public List<ClientJob> GetClientJob(string clientId) {
    clientId = clientId.Split(":")?.First();
    return clientJobMappings.ContainsKey(clientId) ? clientJobMappings[clientId] : null;
  }
}

public class ClientJob {
  public string Code { get; }
  public JobType Type { get; }

  public ClientJob(string code, JobType type) {
    Code = code;
    Type = type;
  }
}

public enum JobType {
  In,
  Out
}
