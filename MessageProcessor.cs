using System.Threading.Tasks;
using Serilog;

namespace RxTcpServer
{
    public static class MessageProcessor
    {
        public static async Task Process(ClientMessage clientMessage)
        {
            // 处理消息时可以访问 IP、当前消息、上一次的消息
            Log.Information("处理消息: [IP: {IP}] [当前消息: {Message}] [上次消息: {LastMessage}]",
                clientMessage.IP, clientMessage.Message, clientMessage.LastMessage);

            // 这里可以添加具体的业务逻辑，例如数据库存储、告警等
            string carrierName;

            if(DataProcessor.IsJsonFormat(clientMessage.Message)){

            }else if(DataProcessor.IsSpecificTextFormat(clientMessage.Message)){
                carrierName = clientMessage.Message;
                var request = new EapJobService();
                   
                if( clientMessage.IP.Contains("10.10.0.101")){
                    await  request.SendJobInRequestAsync("SYSTEM","MCT-SC-A-0038",carrierName);
                }else if( clientMessage.IP.Contains("10.10.0.102")){
                    await  request.SendJobOutRequestAsync("SYSTEM","MCT-SC-A-0038",carrierName);
                }
                else if( clientMessage.IP.Contains("10.10.0.103")){
                    await  request.SendJobInRequestAsync("SYSTEM","MCT-SC-A-0039",carrierName);
                }
                else if( clientMessage.IP.Contains("10.10.0.104")){
                    await  request.SendJobOutRequestAsync("SYSTEM","MCT-SC-A-0039",carrierName);
                    await  request.SendJobInRequestAsync("SYSTEM","MCT-SC-A-0040",carrierName);
                }
                else if( clientMessage.IP.Contains("10.10.0.105")){
                    await  request.SendJobOutRequestAsync("SYSTEM","MCT-SC-A-0040",carrierName);
                    await  request.SendJobInRequestAsync("SYSTEM","MCT-SC-A-0041",carrierName);
                }
                else if( clientMessage.IP.Contains("10.10.0.106")){
                    await  request.SendJobOutRequestAsync("SYSTEM","MCT-SC-A-0041",carrierName);
                    await  request.SendJobInRequestAsync("SYSTEM","MCT-SC-A-0042",carrierName);
                }
                else if( clientMessage.IP.Contains("10.10.0.108")){
                    await  request.SendJobOutRequestAsync("SYSTEM","MCT-SC-A-0042",carrierName);
                }
                else if( clientMessage.IP.Contains("10.10.0.109")){
                    await  request.SendJobInRequestAsync("SYSTEM","MCT-SC-A-0043",carrierName);
                }
                else if( clientMessage.IP.Contains("10.10.0.110")){
                    await  request.SendJobOutRequestAsync("SYSTEM","MCT-SC-A-0043",carrierName);
                    await  request.SendJobInRequestAsync("SYSTEM","MCT-SC-A-0044",carrierName);
                }   else if( clientMessage.IP.Contains("10.10.0.111")){
                    await  request.SendJobOutRequestAsync("SYSTEM","MCT-SC-A-0044",carrierName);
                }
                else if( clientMessage.IP.Contains("10.10.0.112")){
                    await  request.SendJobInRequestAsync("SYSTEM","MCT-SC-A-0045",carrierName);
                }   else if( clientMessage.IP.Contains("10.10.0.107")){ 
                    await  request.SendJobOutRequestAsync("SYSTEM","MCT-SC-A-0045",carrierName);
                    await  request.SendJobInRequestAsync("SYSTEM","MCT-SC-A-0046",carrierName);
                }                   
                else if( clientMessage.IP.Contains("10.10.0.113")){
                    await  request.SendJobOutRequestAsync("SYSTEM","MCT-SC-A-0046",carrierName);
                }
            }else{
                Log.Error("处理消息: [IP: {IP}] [当前消息: {Message}] [上次消息: {LastMessage}]",
                clientMessage.IP, clientMessage.Message, clientMessage.LastMessage);
            }

            await Task.CompletedTask;

        }
    }
}


