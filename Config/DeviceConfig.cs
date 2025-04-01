using System.Collections.Generic;

namespace RxTcpServer
{
    public class DeviceConfig
    {
        public string IP { get; set; }
        public List<string> Jobs { get; set; }
        public bool IsJobOut { get; set; } = false; // 默认 false，表示执行 JobIn
    }
}
