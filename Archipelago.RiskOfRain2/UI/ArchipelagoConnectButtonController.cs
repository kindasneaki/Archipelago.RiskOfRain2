using R2API.Utils;
using RoR2.UI;
using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Archipelago.RiskOfRain2.UI
{

    public class ArchipelagoConnectButtonController : MonoBehaviour
    {
        private CharacterSelectController contr;
        private ArchipelagoPlugin ArchipelagoPlugin;
        public GameObject connectPanel;
        //public ConnectClick connectClick;
        public string assetName = "ConnectPanel";
        public string bundleName = "connectbundle";
        public GameObject chat;
        public GameObject ConnectPanel;
        public GameObject MinimizePanel;
        private string minimizeText = "-";
        private TMP_FontAsset font;

        public delegate string SlotChanged(string newValue);
        public static SlotChanged OnSlotChanged;
        public delegate string PasswordChanged(string newValue);
        public static PasswordChanged OnPasswordChanged;
        public delegate string UrlChanged(string newValue);
        public static UrlChanged OnUrlChanged;
        public delegate string PortChanged(string newValue);
        public static PortChanged OnPortChanged;
        public delegate void ConnectClicked();
        public static ConnectClicked OnConnectClick;
        public static ConnectClicked OnButtonClick;
        public void Start()
        {
            AssetBundle localAssetBundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(ArchipelagoPlugin.Instance.Info.Location), bundleName));
            if (localAssetBundle == null)
            {
                Debug.LogError("Failed to load AssetBundle!");
                    return;
            }
            connectPanel = localAssetBundle.LoadAsset<GameObject>("ConnectCanvas");
            //connectClick = connectPanel.AddComponent<ConnectClick>();
            //connectClick.Connect = connectPanel;
            localAssetBundle.Unload(false);
            On.RoR2.UI.CharacterSelectController.Update += CharacterSelectController_Update;

        }
        public void OnLoadDone(UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<GameObject> obj)
        {
            if(obj.Result == null)
            {
                Log.LogDebug("error obj is null");
            } else
            {
                Log.LogDebug($"obj.Result {obj.Result}");
            }
        }
        public void Awake()
        {
            On.RoR2.UI.CharacterSelectController.Awake += CharacterSelectController_Awake;
            OnButtonClick += ButtonPressed;
            
        }


        private void CharacterSelectController_Update(On.RoR2.UI.CharacterSelectController.orig_Update orig, CharacterSelectController self)
        {
            orig(self);
            contr = self;
            
            if (chat != null && chat.gameObject.activeSelf == false)
            {
                chat.gameObject.SetActive(true);
                //On.RoR2.UI.CharacterSelectController.Update -= CharacterSelectController_Update;                
            }

        }

        //Hook for when the lobby is entered
        internal void CharacterSelectController_Awake(On.RoR2.UI.CharacterSelectController.orig_Awake orig, CharacterSelectController self)
        {
            orig(self);
            contr = self;
            chat = contr.transform.Find("SafeArea/ChatboxPanel/").gameObject;
            CreateButton();
            CreateFields();
            CreateMinimizeButton();
            Log.LogDebug("Charater Awake");
            ConnectPanel = contr.transform.Find("SafeArea/ConnectCanvas(Clone)/Panel").gameObject;
        }
        //Create button for the lobby
        private void CreateButton()
        {
            var readyButton = contr.transform.Find("SafeArea/ReadyPanel/ReadyButton");
            font = readyButton.GetComponentInChildren<TextMeshProUGUI>().font;
            var readyPanel = contr.transform.Find("SafeArea");
            var baseHoverOutlineSprite = readyButton.Find("HoverOutlineImage");

            var cb = Instantiate(connectPanel);
            cb.AddComponent<MPEventSystemLocator>();
            cb.AddComponent<HGGamepadInputEvent>();
            cb.transform.SetParent(readyPanel, false);
            cb.transform.localPosition = new Vector3(125, 0, 0);
            cb.transform.localScale = Vector3.one;
            RectTransform rectTransform = cb.GetComponent<RectTransform>();

            var button = contr.transform.Find("SafeArea/ConnectCanvas(Clone)/Panel/Button/").gameObject;
            button.AddComponent<HGButton>();
            button.AddComponent<HGGamepadInputEvent>();

            var outline = Instantiate(baseHoverOutlineSprite);
            outline.transform.SetParent(button.transform);
            outline.transform.localPosition = button.transform.localPosition;
            button.GetComponent<HGButton>().imageOnHover = outline.GetComponent<Image>();
            button.GetComponent<HGButton>().showImageOnHover = true;
            button.GetComponent<HGButton>().allowAllEventSystems = true;
            button.GetComponent<HGButton>().onClick.AddListener(() => OnButtonClick());
            button.GetComponent<Image>().sprite = readyButton.gameObject.GetComponent<Image>().sprite;
            button.GetComponentInChildren<TextMeshProUGUI>().font = font;
        }
        //Listeners for the fields to save changed info
        private void CreateFields()
        {
            var inputSlotName = contr.transform.Find("SafeArea/ConnectCanvas(Clone)/Panel/InputSlotName/").gameObject;
            inputSlotName.GetComponent<TMP_InputField>().onValueChanged.AddListener((string value) => { OnSlotChanged(value); });
            inputSlotName.GetComponent<TMP_InputField>().text = ArchipelagoPlugin.apSlotName;
            var inputPassword = contr.transform.Find("SafeArea/ConnectCanvas(Clone)/Panel/InputPassword/").gameObject;
            inputPassword.GetComponent<TMP_InputField>().onValueChanged.AddListener((string value) => { OnPasswordChanged(value); });
            inputPassword.GetComponent<TMP_InputField>().text = ArchipelagoPlugin.apPassword;
            var inputUrl = contr.transform.Find("SafeArea/ConnectCanvas(Clone)/Panel/InputUrl/").gameObject;
            inputUrl.GetComponent<TMP_InputField>().onValueChanged.AddListener((string value) => { OnUrlChanged(value); });
            inputUrl.GetComponent<TMP_InputField>().text = ArchipelagoPlugin.apServerUri;
            var inputPort = contr.transform.Find("SafeArea/ConnectCanvas(Clone)/Panel/InputPort/").gameObject;
            inputPort.GetComponent<TMP_InputField>().onValueChanged.AddListener((string value) => { OnPortChanged(value); });
            inputPort.GetComponent<TMP_InputField>().text = string.Concat(ArchipelagoPlugin.apServerPort);
        }
        private void CreateMinimizeButton()
        {
            var minimizePanel = contr.transform.Find("SafeArea/ConnectCanvas(Clone)/Hide");
            var button = contr.transform.Find("SafeArea/ConnectCanvas(Clone)/Hide/Button").gameObject;
            button.AddComponent<HGButton>();
            button.AddComponent<HGGamepadInputEvent>();
            minimizePanel.GetComponentInChildren<TextMeshProUGUI>().font = font;
            button.GetComponent<HGButton>().onClick.AddListener(() => OnButtonClick());
            MinimizePanel = minimizePanel.gameObject;
        }
        private void ButtonPressed()
        {
            ConnectPanel.SetActive(!ConnectPanel.activeSelf);
            minimizeText = (minimizeText == "-") ? "AP +" : "-";
            MinimizePanel.GetComponentInChildren<TextMeshProUGUI>().text = minimizeText;
        }
    }
}
