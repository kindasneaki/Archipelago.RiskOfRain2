using System;
using System.Collections.Generic;
using System.Text;
using Archipelago.RiskOfRain2.Extensions;
using Archipelago.RiskOfRain2.Net;
using RoR2.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Archipelago.RiskOfRain2.UI
{
    public class ArchipelagoLocationCheckProgressBarUI : IDisposable
    {
        public int ItemPickupStep { get; set; }
        public int CurrentItemCount { get; set; }
        public Color CurrentColor { get; set; }

        private HUD hud;
        private ArchipelagoLocationCheckProgressBarController locationCheckBar;
        private GameObject container;
        private Vector2 textoffset;
        private Vector2 baroffset;
        private string textcontent;

        public static readonly Color defaultColor = new Color(.8f, .5f, 1, 1);
        public static readonly Color altColor = new Color(1f, .8f, .5f, 1);

        public ArchipelagoLocationCheckProgressBarUI(Vector2 textoffset, Vector2 baroffset, string text = "Location Check Progress: ")
        {
            On.RoR2.UI.HUD.Awake += HUD_Awake;
            this.textoffset = textoffset;
            this.baroffset = baroffset;
            textcontent = text;
            CurrentColor = defaultColor;
        }

        public void UpdateCheckProgress(int count, int step)
        {
            ItemPickupStep = step;
            CurrentItemCount = count;

            if (locationCheckBar != null)
            {
                locationCheckBar.steps = step;
                locationCheckBar.fill = count;
            }
        }

        public void ChangeBarColor(Color newcolor)
        {
            CurrentColor = newcolor;

            if (locationCheckBar != null)
            {
                locationCheckBar.color = newcolor;
            }
        }

        public void Dispose()
        {
            hud = null;
            On.RoR2.UI.HUD.Awake -= HUD_Awake;

            GameObject.Destroy(container);
        }

        private void HUD_Awake(On.RoR2.UI.HUD.orig_Awake orig, HUD self)
        {
            orig(self);
            hud = self;
            PopulateHUD();
        }

        private void PopulateHUD()
        {
            var container = new GameObject("ArchipelagoHUD");

            var text = CreateTextLabel();
            text.transform.SetParent(container.transform);
            text.transform.ResetScaleAndRotation();

            var progressBar = CreateProgressBar();
            progressBar.transform.SetParent(container.transform);
            progressBar.transform.ResetScaleAndRotation();
            progressBar.GetComponent<RectTransform>().anchoredPosition = baroffset;

            var rectTransform = container.AddComponent<RectTransform>();
            container.transform.SetParent(hud.expBar.transform.parent.parent);
            rectTransform.ResetAnchorsAndOffsets();
            rectTransform.anchoredPosition = textoffset;
            container.transform.ResetScaleAndRotation();

            locationCheckBar.canvas.SetColor(CurrentColor);
            locationCheckBar.color = CurrentColor;

            this.container = container;
        }

        private GameObject CreateTextLabel()
        {
            var container = new GameObject("ArchipelagoTextLabel");
            var rect = container.AddComponent<RectTransform>();
            rect.anchoredPosition = Vector2.zero;
            rect.ResetAnchorsAndOffsets();

            var text = GameObject.Instantiate(hud.levelText.targetText);
            text.text = textcontent;
            text.transform.SetParent(container.transform);
            text.transform.ResetScaleAndRotation();

            var textRect = text.GetComponent<RectTransform>();
            textRect.ResetAnchorsAndOffsets();
            textRect.anchoredPosition = new Vector2(-85f, -2f);

            return container;
        }

        private GameObject CreateProgressBar()
        {
            var progressBarGameObject = GameObject.Instantiate(hud.expBar.gameObject);
            GameObject.Destroy(progressBarGameObject.GetComponent<ExpBar>());

            RectTransform rectTransform = progressBarGameObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = Vector2.right;
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.offsetMin = new Vector2(250f, 0f);
            rectTransform.offsetMax = new Vector2(0f, 4f);

            locationCheckBar = progressBarGameObject.AddComponent<ArchipelagoLocationCheckProgressBarController>();
            locationCheckBar.fill = CurrentItemCount;
            locationCheckBar.steps = ItemPickupStep;

            var fillPanel = progressBarGameObject.transform.Find("ShrunkenRoot/FillPanel");
            locationCheckBar.fillRectTransform = fillPanel.GetComponent<RectTransform>();

            var canvas = fillPanel.GetComponent<CanvasRenderer>();
            locationCheckBar.canvas = canvas;

            return progressBarGameObject;
        }
    }
}
