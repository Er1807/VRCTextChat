using TextChat;
using MelonLoader;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnhollowerRuntimeLib;
using Il2CppSystem.Collections.Generic;
using System.Collections;
using VRCWSLibary;
using Newtonsoft.Json;
using UnityEngine.Events;
using System.Threading.Tasks;
using VRC.UI;
using UnhollowerRuntimeLib.XrefScans;
using System.Linq;
using System.Reflection;
using UnhollowerBaseLib.Attributes;

[assembly: MelonInfo(typeof(TextChatMod), "TextChat", "1.1.1", "Eric van Fandenfart")]
[assembly: MelonGame]
[assembly: MelonAdditionalDependencies("VRCWSLibary")]

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

        private MethodInfo openMessageBox;

        public override void OnApplicationStart()
        {
            MelonLogger.Msg($"Actionmenu initialised");
            openMessageBox = FindPopupMethod();
            MelonCoroutines.Start(LoadClient());

            MelonCoroutines.Start(WaitForUIInit());
        }

        public IEnumerator LoadClient()
        {
            while (!Client.ClientAvailable())
                yield return null;

            MelonLogger.Msg($"Client Available");

            client = Client.GetClient();
            client.RegisterEvent("SendMessageTo", (msg) => {
                MelonLogger.Msg($"Recieved message from {msg.Target}, with content {msg.Content}");
                AsyncUtilsVRCWS.ToMain(() => {
                    TextMessage textMsg = msg.GetContentAs<TextMessage>();
                    openMessageBox.Invoke(VRCUiPopupManager.prop_VRCUiPopupManager_0, new object[] { $"Message recieved from {textMsg.Username}", textMsg.Message, "OK", new Action(() => { VRCUiManager.prop_VRCUiManager_0.HideScreen("POPUP"); }), null });

                });

            }, false);

            client.MethodCheckResponseRecieved += (method, userid, accept) => {
                if (PageUserInfo.field_Internal_Static_String_1 == userid && method == "SendMessageTo" && accept)
                    button.interactable = true;
            };

            MelonLogger.Msg($"Setup Client");
        }

        private MethodInfo FindPopupMethod()
        {
            return typeof(VRCUiPopupManager)
                .GetMethods()
                .Where(x => x.Name.StartsWith("Method_Public_Void_String_String_String_Action_Action_1_VRCUiPopup"))
                .Where(x=> XrefScanner.XrefScan(x).Any(y => y.ReadAsObject().ToString() == "UserInterface/MenuContent/Popups/StandardPopupV2")).SingleOrDefault();
            
                
            
        }


        private IEnumerator WaitForUIInit()
        {
            while (VRCUiManager.prop_VRCUiManager_0 == null)
                yield return null;
            while (GameObject.Find("UserInterface").transform.Find("Canvas_QuickMenu(Clone)/Container/Window/QMParent") == null)
                yield return null;

            menu = GameObject.Find("UserInterface/MenuContent/Screens/UserInfo/");
            var baseUIElement = GameObject.Find("UserInterface/MenuContent/Screens/UserInfo/Buttons/RightSideButtons/RightUpperButtonColumn/PlaylistsButton").gameObject;

            var gameObject = GameObject.Instantiate(baseUIElement, baseUIElement.transform.parent, true);
            gameObject.name = "Send_Message";

            var uitext = gameObject.GetComponentInChildren<Text>();
            uitext.text = "Send Message";

            button = gameObject.GetComponent<Button>();
            button.onClick = new Button.ButtonClickedEvent();
            var action = new Action(delegate () {
                GetMessage(PageUserInfo.field_Internal_Static_String_1);
            });
            button.onClick.AddListener(DelegateSupport.ConvertDelegate<UnityAction>(action));


            EnableDisableListener listener = menu.AddComponent<EnableDisableListener>();

            listener.OnEnableEvent += () => MelonCoroutines.Start(DelayedCheck());

            MelonLogger.Msg("Buttons sucessfully created");
        }

        public IEnumerator DelayedCheck()
        {
            button.interactable = false;
            yield return null;
            Task.Run(async() => {
                try
                {
                    bool response = await client.DoesUserAcceptMethodAsyncResponse(PageUserInfo.field_Internal_Static_String_1, "SendMessageTo");

                    button.interactable = response;
                }
                catch (TimeoutException)
                {
                    MelonLogger.Msg("Didnt recieve a valid response in time");
                }
                
            });
        }

        public void SendMessageTo(string userID, string message)
        {
            client.Send(new Message() { Method = "SendMessageTo", Target = userID, Content = JsonConvert.SerializeObject(new TextMessage() { Username = VRCPlayer.field_Internal_Static_VRCPlayer_0.prop_String_1, Message=message }) });
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

    //https://github.com/loukylor/VRC-Mods/blob/VRCUK-1.1.0/VRChatUtilityKit/Components/EnableDisableListener.cs
    [RegisterTypeInIl2Cpp]
    public class EnableDisableListener : MonoBehaviour
    {
        [method: HideFromIl2Cpp]
        public event Action OnEnableEvent;
        [method: HideFromIl2Cpp]
        public event Action OnDisableEvent;
        public EnableDisableListener(IntPtr obj0) : base(obj0) { }

        internal void OnEnable() => OnEnableEvent?.Invoke();
        internal void OnDisable() => OnDisableEvent?.Invoke();
    }
}
