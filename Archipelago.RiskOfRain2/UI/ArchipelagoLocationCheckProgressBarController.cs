using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Archipelago.RiskOfRain2.UI
{
    public class ArchipelagoLocationCheckProgressBarController : MonoBehaviour
    {
        public int fill;

        public int steps;

        public Color color;

        public RectTransform fillRectTransform;

        public CanvasRenderer canvas;

        public void Update()
        {
            // -1 so that the bar appears full when a check is next.
            var progressPercent = Mathf.InverseLerp(0, steps-1, fill);
            if (fillRectTransform)
            {
                fillRectTransform.anchorMin = new Vector2(0f, 0f);
                fillRectTransform.anchorMax = new Vector2(progressPercent, 1f);
                fillRectTransform.sizeDelta = new Vector2(1f, 1f);
            }
            if (canvas)
            {
                canvas.SetColor(color);
            }
        }
    }
}
