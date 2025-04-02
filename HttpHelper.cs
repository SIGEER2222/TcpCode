using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Serilog;

public static class HttpHelper {
  private static readonly HttpClient _client = new HttpClient();
  private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase, // 使用驼峰命名
    WriteIndented = false // 不缩进，提高传输效率
  };

  /// <summary>
  /// 发送 HTTP POST 请求，提交 JSON 数据
  /// </summary>
  public static async Task<string> PostJsonAsync<T>(string url, T data) {
    string json = JsonSerializer.Serialize(data, _jsonOptions);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    try {
      HttpResponseMessage response = await _client.PostAsync(url, content);
      string responseContent = await response.Content.ReadAsStringAsync();

      Log.Information(responseContent);

      return responseContent;
    }
    catch (HttpRequestException httpEx) // 捕获 HTTP 请求异常
    {
      Log.Error("HTTP 请求异常: {Message}, 状态码: {StatusCode}, 详细信息: {Content}",
                 httpEx.Message, httpEx.StatusCode, httpEx.InnerException?.Message);
      return $"HTTP 请求异常: {httpEx.Message}";
    }
    catch (TaskCanceledException taskEx) // 捕获超时异常
    {
      Log.Error("请求超时: {Message}, 详细信息: {Details}",
                 taskEx.Message, taskEx.InnerException?.Message);
      return $"请求超时: {taskEx.Message}";
    }
    catch (Exception ex) // 捕获所有其他异常
    {
      Log.Error("请求失败: {Message}, 详细信息: {Details}, StackTrace: {StackTrace}",
                 ex.Message, ex.InnerException?.Message, ex.StackTrace);
      return $"请求失败: {ex.Message}";
    }
  }
}

/// <summary>
/// 作业请求服务
/// </summary>
public static class EapJobService {
  private static readonly string _jobInUrl = "http://10.10.4.203:31000/operation/api/app/eap-operation/e-aP_Job-prep_Job-in_Check";
  private static readonly string _jobOutUrl = "http://10.10.4.203:31000/operation/api/app/eap-operation/e-aP_Job-prep_Job-out_Check";

  public static async Task<string> SendJobInRequestAsync(string eqpName, string carrierName) {
    return await SendJobRequestAsync(_jobInUrl, "SYSTEM", eqpName, carrierName);
  }

  public static async Task<string> SendJobOutRequestAsync(string eqpName, string carrierName) {
    return await SendJobRequestAsync(_jobOutUrl, "SYSTEM", eqpName, carrierName);
  }
  /// <summary>
  /// 发送 Job In 请求
  /// </summary>
  public static async Task<string> SendJobInRequestAsync(string userName, string eqpName, string carrierName) {
    return await SendJobRequestAsync(_jobInUrl, userName, eqpName, carrierName);
  }

  /// <summary>
  /// 发送 Job Out 请求
  /// </summary>
  public static async Task<string> SendJobOutRequestAsync(string userName, string eqpName, string carrierName) {
    return await SendJobRequestAsync(_jobOutUrl, userName, eqpName, carrierName);
  }

  /// <summary>
  /// 统一的作业请求发送方法
  /// </summary>
  private static async Task<string> SendJobRequestAsync(string url, string userName, string eqpName, string carrierName) {
    var requestData = new {
      userName,
      eqpName,
      carrierName
    };

    return await HttpHelper.PostJsonAsync(url, requestData);
  }
}
