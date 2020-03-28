using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
using UnityEngine;

[NPL.PluginAttr("nimgame")]
public static class NimGame
{
    [NPL.PluginFunctionAttr("UnityPluginLoad")] 
    public static UnityPluginLoadDel UnityPluginLoad = null;
    public delegate void UnityPluginLoadDel(IntPtr unityInterfaces);

    [NPL.PluginFunctionAttr("UnityPluginUnload")] 
    public static UnityPluginUnloadDel UnityPluginUnload = null;
    public delegate void UnityPluginUnloadDel();

    [NPL.PluginFunctionAttr("GetRenderEventFunc")] 
    public static GetRenderEventFuncDel GetRenderEventFunc = null;
    public delegate IntPtr GetRenderEventFuncDel();

    [NPL.PluginFunctionAttr("TriggerMessage")] 
    public static TriggerMessageDel TriggerMessage = null;
    public delegate void TriggerMessageDel();

    [NPL.PluginFunctionAttr("SetMessageHandler")] 
    public static SetMessageHandlerDel SetMessageHandler = null;

    [NPL.PluginFunctionAttr("OnMessage")] 
    public static MessageHandlerDel OnMessage = null;
   
    public delegate void MessageHandlerDel(IntPtr data, int len);
    public delegate void SetMessageHandlerDel(MessageHandlerDel handler);
}

public class Test : MonoBehaviour
{
    private NimGame.MessageHandlerDel msgHandler;   // Ensure it doesn't get garbage collected
   
    // Start is called before the first frame update
    void Start()
    {
        msgHandler = new NimGame.MessageHandlerDel(OnMessage);
        NimGame.SetMessageHandler(msgHandler);
     }

    // Update is called once per frame
    void Update()
    {
        if( Input.GetMouseButtonDown(0) )
        {
            NimGame.TriggerMessage();
        }
    }

    public void OnMessage(IntPtr data, int len)
    {
        var ms = new MessageStream(data, len);
        var reader = ms.BeginRead();
        var hellofromnim = reader.read_string();
        Debug.Log(hellofromnim);
    }
}
