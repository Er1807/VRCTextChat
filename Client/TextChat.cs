using ActionMenuApi.Api;
using TextChat;
using MelonLoader;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRChatUtilityKit.Ui;
using VRChatUtilityKit.Utilities;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

[assembly: MelonInfo(typeof(TextChatMod), "TextChat", "1.0.0", "Eric van Fandenfart")]
[assembly: MelonGame]

namespace TextChat
{

    public class TextChatMod : MelonMod
    {
        private ClientWebSocket ws;

        public override void OnApplicationStart()
        {
            ws = new ClientWebSocket();
            ws.ConnectAsync(new Uri("ws://localhost:8080"), CancellationToken.None);
            Task.Run(() => {
            Console.WriteLine("Task thread ID: {0}",
                 Thread.CurrentThread.ManagedThreadId);
                recieve();
            });
            VRCActionMenuPage.AddSubMenu(ActionMenuPage.Main,
                   "Freeze Frame Animation3",
                   delegate {
                       MelonLogger.Msg("Freeze Frame Menu Opened");
                       CustomSubMenu.AddButton("test", () => Test());
                       CustomSubMenu.AddButton("test3", () => ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("sendMessageTo 1 Hi_from_VRC")), WebSocketMessageType.Text, true, CancellationToken.None));
                   }
               );

            MelonLogger.Msg($"Actionmenu initialised");
        }

        private void Test()
        {
            string userID = VRCPlayer.field_Internal_Static_VRCPlayer_0.prop_String_2;
            ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes($"startConnection {userID}")), WebSocketMessageType.Text, true, CancellationToken.None);
            
        }

        private async void recieve()
        {
            const int maxMessageSize = 1024;
            byte[] receiveBuffer = new byte[maxMessageSize];
            while (ws.State == WebSocketState.Open)
            {
                WebSocketReceiveResult receiveResult = await ws.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);
                if(receiveResult.MessageType == WebSocketMessageType.Text)
                {
                    var receivedString = Encoding.UTF8.GetString(receiveBuffer, 0, receiveResult.Count);
                    MelonLogger.Msg(receivedString);
                }
            }
        }

        public override void OnUpdate()
        {
            
        }


    }
}
