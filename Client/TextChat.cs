﻿using TextChat;
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
using VRCWSLibary;
using Newtonsoft.Json;
using UnityEngine.Events;

[assembly: MelonInfo(typeof(TextChatMod), "TextChat", "1.0.2", "Eric van Fandenfart")]
[assembly: MelonGame]
[assembly: MelonAdditionalDependencies("VRChatUtilityKit", "VRCWSLibary")]

namespace TextChat
{

    public class TextMessage
    {
        public string Username;
        public string Message;
    }
    public class TextChatMod : MelonMod
    {
        Client client;
        private Button button;
        private GameObject menu;

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
                TextMessage textMsg = msg.GetContentAs<TextMessage >();
                UiManager.OpenSmallPopup($"Message recieved from {textMsg.Username}", textMsg.Message, "OK", new Action(() => { UiManager.ClosePopup(); }));

            }, false);

            client.MethodCheckResponseRecieved += (userid, method, accept) => {
                if (VRCUtils.ActiveUserInUserInfoMenu?.id == userid && method == "SendMessageTo" && accept)
                    button.interactable = true;
            };

            MelonLogger.Msg($"Setup Client");
        }




        private void Init()
        {
            menu = GameObject.Find("UserInterface/MenuContent/Screens/UserInfo/");
            var baseUIElement = GameObject.Find("UserInterface/MenuContent/Screens/UserInfo/Buttons/RightSideButtons/RightUpperButtonColumn/PlaylistsButton").gameObject;

            var gameObject = GameObject.Instantiate(baseUIElement, baseUIElement.transform.parent, true);
            gameObject.name = "Send_Message";

            var uitext = gameObject.GetComponentInChildren<Text>();
            uitext.text = "Send Message";

            button = gameObject.GetComponent<Button>();
            button.onClick = new Button.ButtonClickedEvent();
            var action = new Action(delegate () {
                GetMessage(VRCUtils.ActiveUserInUserInfoMenu.id);
            });
            button.onClick.AddListener(DelegateSupport.ConvertDelegate<UnityAction>(action));


            EnableDisableListener listener = menu.AddComponent<EnableDisableListener>();

            listener.OnEnableEvent += () => { RunMethodCheck(); };

            MelonLogger.Msg("Buttons sucessfully created");
        }

       

        

        private void RunMethodCheck()
        {
            button.interactable = false;
            MelonCoroutines.Start(DelayedCheck());
        }

        public IEnumerator DelayedCheck()
        {
            yield return null;
            client.DoesUserAcceptMethod(VRCUtils.ActiveUserInUserInfoMenu.id, "SendMessageTo");
        }

        public void SendMessageTo(string userID, string message)
        {
            client.Send(new Message() { Method = "SendMessageTo", Target = userID, Content = JsonConvert.SerializeObject(new TextMessage() { Username = VRCPlayer.field_Internal_Static_VRCPlayer_0.prop_String_0, Message=message }) });
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
