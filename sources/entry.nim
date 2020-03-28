
{.emit"""
#include "IUnityInterface.h"
#include "IUnityGraphics.h"

N_LIB_EXPORT N_CDECL(void, NimMain)(void);
extern "C" void game_init();
extern "C" void game_shutdown();

static IUnityGraphics* s_Graphics = NULL;
    
static void UNITY_INTERFACE_API
    OnGraphicsDeviceEvent(UnityGfxDeviceEventType eventType)
{
    switch (eventType)
    {
        case kUnityGfxDeviceEventInitialize:
        {
            game_init();
            break;
        }
        case kUnityGfxDeviceEventShutdown:
        {
            game_shutdown();
            break;
        }
        case kUnityGfxDeviceEventBeforeReset:
        {
            break;
        }
        case kUnityGfxDeviceEventAfterReset:
        {
            break;
        }
    };
}

// Unity plugin load event
extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API
    UnityPluginLoad(IUnityInterfaces* unityInterfaces)
{
    NimMain();
    UnityInterfaceGUID guid = UNITY_GET_INTERFACE_GUID(IUnityGraphics);
    s_Graphics = (IUnityGraphics*)unityInterfaces->GetInterface(guid);
    s_Graphics->RegisterDeviceEventCallback(OnGraphicsDeviceEvent);

    OnGraphicsDeviceEvent(kUnityGfxDeviceEventInitialize);
}
    
// Unity plugin unload event
extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API
    UnityPluginUnload()
{
    s_Graphics->UnregisterDeviceEventCallback(OnGraphicsDeviceEvent);
}
""".}
