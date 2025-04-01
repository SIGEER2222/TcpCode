using System.Collections.Concurrent;

namespace RxTcpServer
{
    public static class IPTracker
    {
        private static readonly ConcurrentDictionary<string, string> LastMessages = new();

        /// <summary>
        /// 获取 IP 最后记录的消息
        /// </summary>
        public static string GetLastMessage(string ip)
        {
            return LastMessages.TryGetValue(ip, out var lastMessage) ? lastMessage : null;
        }

        /// <summary>
        /// 更新 IP 对应的消息
        /// </summary>
        public static void Update(string ip, string message)
        {
            LastMessages[ip] = message;
        }

        /// <summary>
        /// 客户端断开连接时，移除其缓存消息
        /// </summary>
        public static void Remove(string ip)
        {
            LastMessages.TryRemove(ip, out _);
        }
    }
}
