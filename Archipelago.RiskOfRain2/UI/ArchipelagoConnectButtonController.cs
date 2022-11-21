using R2API.Utils;
using RoR2.UI;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Archipelago.RiskOfRain2.UI
{

    public class ArchipelagoConnectButtonController : MonoBehaviour
    {
        private CharacterSelectController contr;
        public GameObject connectPanel;
        public ConnectClick connectClick;
        public UnityAction ConnectButtonClicked;
        public string assetName = "ConnectPanel";
        public string bundleName = "connectbundle";
        public void Start()
        {
            ConnectButtonClicked += ButtonTest;
            AssetBundle localAssetBundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(ArchipelagoPlugin.Instance.Info.Location), bundleName));
            if (localAssetBundle == null)
            {
                Debug.LogError("Failed to load AssetBundle!");
                    return;
            }
            connectPanel = localAssetBundle.LoadAsset<GameObject>("ConnectCanvas");
            connectClick = connectPanel.AddComponent<ConnectClick>();
            connectClick.Connect = connectPanel;
            localAssetBundle.Unload(false);


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
            Log.LogDebug("Awake()");
            On.RoR2.UI.CharacterSelectController.Awake += CharacterSelectController_Awake;
        }
        internal void CharacterSelectController_Awake(On.RoR2.UI.CharacterSelectController.orig_Awake orig, CharacterSelectController self)
        {
            orig(self);
            contr = self;
            var showChat = contr.transform.Find("SafeArea/ChatboxPanel");
            showChat.gameObject.SetActive(true);
            CreateButton();

        }
        public void ButtonTest()
        {
            Log.LogDebug("Button Pressed");
            ChatMessage.Send("Button Pressed");
        }
        private void CreateButton()
        {
            var readyButton = contr.transform.Find("SafeArea/ReadyPanel/ReadyButton");
            var readyPanel = contr.transform.Find("SafeArea");
            var baseHoverOutlineSprite = readyButton.Find("HoverOutlineImage");

            var cb = Instantiate(connectPanel);
            cb.AddComponent<MPEventSystemLocator>();
            cb.AddComponent<HGGamepadInputEvent>();
            cb.transform.SetParent(readyPanel, false);
            cb.transform.localPosition = new Vector3(125, 0, 0);
            cb.transform.localScale = Vector3.one;
            RectTransform rectTransform = cb.GetComponent<RectTransform>();
            //rectTransform.anchoredPosition = Vector2.zero;

            var button = contr.transform.Find("SafeArea/ConnectCanvas(Clone)/Panel/Button/").gameObject;
            Log.LogDebug("buttonPanel");
            //var button = buttonPanel.transform.GetChild(0).gameObject;
            button.AddComponent<HGButton>();
            button.AddComponent<HGGamepadInputEvent>();
            button.GetComponent<HGGamepadInputEvent>().actionName = "ButtonTest";

            var outline = Instantiate(baseHoverOutlineSprite);
            outline.transform.SetParent(button.transform);
            outline.transform.localPosition = button.transform.localPosition;
            button.GetComponent<HGButton>().imageOnHover = outline.GetComponent<Image>();
            button.GetComponent<HGButton>().showImageOnHover = true;
            button.GetComponent<HGButton>().allowAllEventSystems = true;
            button.GetComponent<Image>().sprite = readyButton.gameObject.GetComponent<Image>().sprite;



        }
    }
}
