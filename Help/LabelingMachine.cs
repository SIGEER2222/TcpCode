using System.Text.Json;
using System;
using System.Linq;
using Serilog;

public class LabelingMachine //贴标机
{
  public int Position { get; set; }  // 点位
  public string Status { get; set; }    // NG还是OK
  public string SerialNumber { get; set; } // SN码
}

public class DispensingMachine //点胶机
{
  public int Position { get; set; }  // 点位
  public string Status { get; set; }    // NG还是OK
  public string ImagePath { get; set; } // 图片路径
}

public class DataProcessor {
  // 判断是否为JSON格式
  public static bool IsJsonFormat(string data) {
    data = data.Trim();
    return (data.StartsWith("{") && data.EndsWith("}")) ||
           (data.StartsWith("[") && data.EndsWith("]"));
  }

  // 处理JSON数据
  private static void ProcessJsonData(string jsonData) {
    var labelingMachines = DeserializeLabelingMachineJson(jsonData);
    if (labelingMachines != null) {
      foreach (var machine in labelingMachines) {
        Log.Information($"Labeling Machine - Position: {machine.Position}, Status: {machine.Status}, SerialNumber: {machine.SerialNumber}");
      }
      return;
    }

    var dispensingMachines = DeserializeDispensingMachineJson(jsonData);
    if (dispensingMachines != null) {
      foreach (var machine in dispensingMachines) {
        Log.Information($"Dispensing Machine - Position: {machine.Position}, Status: {machine.Status}, ImagePath: {machine.ImagePath}");
      }
      return;
    }

    Log.Information("Unrecognized JSON format.");
  }

  // 处理文本格式数据的方法，这里根据新格式进行处理
  public static void ProcessTextFormat(string textData) {
    // 检查是否为 300301-0001-018 格式
    if (IsSpecificTextFormat(textData)) {
      ProcessSpecificTextFormat(textData);
    }
    else {
      // 其他文本格式，简单打印
      Log.Information($"Received text data: {textData}");
    }
  }

  // 判断是否为 300301-0001-018 格式
  public static bool IsSpecificTextFormat(string data) {
    var parts = data.Split('-').ToList();
    return parts.Count == 3 && parts.All(part => part.Length > 0);
  }

  // 处理 300301-0001-018 格式的数据
  private static void ProcessSpecificTextFormat(string textData) {
    string[] parts = textData.Split('-');
    Log.Information($"Processed specific text format - Part1: {parts[0]}, Part2: {parts[1]}, Part3: {parts[2]}");
  }

  // 处理LabelingMachine类型的JSON数据
  public static LabelingMachine[]? DeserializeLabelingMachineJson(string jsonData) {
    try {
      return JsonSerializer.Deserialize<LabelingMachine[]>(jsonData);
    }
    catch (JsonException ex) {
      Log.Information($"Error deserializing LabelingMachine JSON: {ex.Message}");
      return null;
    }
  }

  // 处理DispensingMachine类型的JSON数据
  public static DispensingMachine[]? DeserializeDispensingMachineJson(string jsonData) {
    try {
      return JsonSerializer.Deserialize<DispensingMachine[]>(jsonData);
    }
    catch (JsonException ex) {
      Log.Information($"Error deserializing DispensingMachine JSON: {ex.Message}");
      return null;
    }
  }
}
