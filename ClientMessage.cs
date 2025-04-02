namespace RxTcpServer {
  public class ClientMessage {
    public string IP { get; }
    public string Message { get; }
    public string LastMessage { get; }

    public ClientMessage(string ip, string message, string lastMessage) {
      IP = ip;
      Message = message;
      LastMessage = lastMessage;
    }
  }
}
