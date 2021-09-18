using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TextChat
{
    class Client
    {
        private ClientWebSocket ws;
        public event MessageEvent MessageRecieved;
        public event OnlineEvent OnlineRecieved;

        public delegate void MessageEvent(string userid, string message);
        public delegate void OnlineEvent(string userid, bool online);
        public Client(string server)
        {
            ws = new ClientWebSocket();
            ws.ConnectAsync(new Uri("ws://localhost:8080"), CancellationToken.None).Wait();
            Task.Run(() => {
                Recieve();
            });
        }

        public void SendMessageTo(string userID, string message)
        {
            Send($"sendMessageTo {userID} {Convert.ToBase64String(Encoding.UTF8.GetBytes(message))}");
        }

        public void IsUserOnline(string userID)
        {
            Send($"isOnline {userID}");
        }

        public void Send(string msg)
        {
            if(ws.State == WebSocketState.Open)
            {
                ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(msg)), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }
        public void SetUserID()
        {
            string userID = VRCPlayer.field_Internal_Static_VRCPlayer_0.prop_String_2;
            ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes($"startConnection {userID}")), WebSocketMessageType.Text, true, CancellationToken.None);

        }
        private async void Recieve()
        {
            const int maxMessageSize = 1024;
            byte[] receiveBuffer = new byte[maxMessageSize];
            while (ws.State == WebSocketState.Open)
            {
                WebSocketReceiveResult receiveResult = await ws.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);
                if (receiveResult.MessageType == WebSocketMessageType.Text)
                {
                    var receivedString = Encoding.UTF8.GetString(receiveBuffer, 0, receiveResult.Count);
                    MelonLogger.Msg(receivedString);
                    string[] arr = receivedString.Split(' ');
                    if (arr[0] == "message")
                    {
                        MessageRecieved?.Invoke(arr[1], Encoding.UTF8.GetString(Convert.FromBase64String(arr[2])));
                    }else if (arr[0] == "status")
                    {
                        OnlineRecieved?.Invoke(arr[1], arr[2] == "online");
                    }
                }
            }
        }
    }
}
