using Archipelago.RiskOfRain2;
using RoR2.UI;
using System;
using UnityEngine;

public class ConnectClick : MonoBehaviour
{
    public GameObject Connect;
    public bool ConnectSet;
    public delegate void ButtonClicked();
    public static ButtonClicked OnButtonClick;
    public ArchipelagoPlugin archipelagoPlugin;
    public ConnectClick()
    {

    }
    public static void ButtonPressed()
    {
        Debug.LogError("Button Pressed");
        Log.LogDebug("button pressed");
        if(OnButtonClick != null)
        {
            OnButtonClick();
        }
    }
    public void Awake()
    {
        archipelagoPlugin = new ArchipelagoPlugin();
        ConnectSet = false;
    }
    public void Update()
    {
        if(Connect != null && !ConnectSet)
        {
            Connect.GetComponentInChildren<HGButton>().onClick.AddListener(ButtonPressed);
            ConnectSet = true;
        }
    }
}
