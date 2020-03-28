import UnityInterface
export UnityInterface

type
    UnityGfxRenderer* = enum 
        kUnityGfxRendererD3D11 = 2
        kUnityGfxRendererNull = 4
        kUnityGfxRendererOpenGLES20 = 8
        kUnityGfxRendererOpenGLES30 = 11
        kUnityGfxRendererPS4 = 13
        kUnityGfxRendererXboxOne = 14
        kUnityGfxRendererMetal = 16
        kUnityGfxRendererOpenGLCore = 17
        kUnityGfxRendererD3D12 = 18
        kUnityGfxRendererVulkan = 21
        kUnityGfxRendererNvn = 22
        kUnityGfxRendererXboxOneD3D12 = 23

    UnityGfxDeviceEventType* = enum 
        kUnityGfxDeviceEventInitialize = 0
        kUnityGfxDeviceEventShutdown = 1
        kUnityGfxDeviceEventBeforeReset = 2
        kUnityGfxDeviceEventAfterReset = 3

    IUnityGraphicsDeviceEventCallback* = proc (eventType:UnityGfxDeviceEventType) : void  {.cdecl.} 

    IUnityGraphics* {.bycopy.} = object 
        GetRenderer* : proc () : UnityGfxRenderer {.cdecl.}
        RegisterDeviceEventCallback* : proc (callback:IUnityGraphicsDeviceEventCallback) : void
        UnregisterDeviceEventCallback* : proc (callback:IUnityGraphicsDeviceEventCallback) : void
        ReserveEventIDRange* : proc (count:cint) : cint {.cdecl.}

type
    PUnityGraphics* = ptr IUnityGraphics
    UnityRenderingEvent* = proc (eventId:cint) : void  {.cdecl.} 
    UnityRenderingEventAndData* = proc (eventId:cint,data:pointer) : void  {.cdecl.} 
 

proc GetUnityInterfaceGUID*[T:PUnityGraphics](): auto = 
    return UnityInterfaceGUID(m_GUIDHigh:0x7CBA0A9CA4DDB544'u64, m_GUIDLow:0x8C5AD4926EB17B11'u64)

proc GetUnityInterfaceGUID*(T:typedesc) : auto = 
    return GetUnityInterfaceGUID[T]()

proc Get*(interf:ptr IUnityInterfaces, T:typedesc) : T =
    return cast[T](interf.GetInterface(GetUnityInterfaceGUID(T)))
