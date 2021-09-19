using ActionMenuApi.Api;
using TextChat;
using MelonLoader;
using System;
using UnityEngine;
using VRChatUtilityKit.Ui;
using VRChatUtilityKit.Utilities;
using UnityEngine.UI;
using UnhollowerRuntimeLib;
using Il2CppSystem.Collections.Generic;
using VRChatUtilityKit.Components;
using System.Collections;
using WSConnectionLibary;

[assembly: MelonInfo(typeof(TextChatMod), "TextChat", "1.0.0", "Eric van Fandenfart")]
[assembly: MelonGame]

namespace TextChat
{


    public class TextChatMod : MelonMod
    {
        Client client;
        private SingleButton button;

        GameObject menu;
        public override void OnApplicationStart()
        {
            MelonLogger.Msg($"Actionmenu initialised");
            MelonCoroutines.Start(LoadClient());
            
            VRCUtils.OnUiManagerInit += Init;
        }

        public IEnumerator LoadClient()
        {
            while (!Client.ClientAvailable())
                yield return null;

            MelonLogger.Msg($"Client Available");

            client = Client.GetClient();

            client.RegisterEvent("SendMessageTo", async (msg) => {
                MelonLogger.Msg($"Recieved message from {msg.Target}, with content {msg.Content}");
                await AsyncUtils.YieldToMainThread();
                UiManager.OpenSmallPopup($"Message recieved from {msg.Target}", msg.Content, "OK", new Action(() => { }));

            });

            client.OnlineRecieved += (userid, online) => {
                if (VRCUtils.ActiveUserInUserInfoMenu?.id == userid && online)
                    button.ButtonComponent.interactable = true;
            };

            MelonLogger.Msg($"Setup Client");
        }




        private void Init()
        {
            menu = GameObject.Find("UserInterface/MenuContent/Screens/UserInfo/");

            button = new SingleButton(menu,
                            new Vector3(3, 4), "Send\r\nMessage", delegate
                            {
                                GetMessage(VRCUtils.ActiveUserInUserInfoMenu.id);
                            },
                            "Send Message to user",
                            "SendMessageSingleBtn");

            button.gameObject.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            button.gameObject.transform.localPosition = new Vector3(0, -380, 0);

            EnableDisableListener listener =  menu.AddComponent<EnableDisableListener>();

            listener.OnEnableEvent += () => { RunOnlineCheck(); };
            MelonLogger.Msg("Buttons sucessfully created");
        }

       

        

        private void RunOnlineCheck()
        {
            button.ButtonComponent.interactable = false;
            MelonCoroutines.Start(DelayedCheck());
        }

        public IEnumerator DelayedCheck()
        {
            yield return null;
            client.IsUserOnline(VRCUtils.ActiveUserInUserInfoMenu.id);
        }

        public void SendMessageTo(string userID, string message)
        {
            client.Send(new Message() { Method = "SendMessageTo", Target = userID, Content = message });
        }
        private void GetMessage(string id)
        {
            //https://github.com/Er1807/VRChatVibratorController/blob/main/VRChatVibratorController/VibratorController.cs#L214
            VRCUiPopupManager.field_Private_Static_VRCUiPopupManager_0.Method_Public_Void_String_String_InputType_Boolean_String_Action_3_String_List_1_KeyCode_Text_Action_String_Boolean_Action_1_VRCUiPopup_Boolean_Int32_0("TextChat", "Message", InputField.InputType.Standard, false/*true = numpad*/, "Send",
                DelegateSupport.ConvertDelegate<Il2CppSystem.Action<string, List<KeyCode>, Text>>(
                        (Action<string, List<KeyCode>, Text>)delegate (string message, List<KeyCode> k, Text t)
                        {
                            SendMessageTo(id, message);
                        }), new Action(() => { }));
        }

    }
}
