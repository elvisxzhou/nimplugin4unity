const
  headername = "IUnityInterface.h"

type
  UnityInterfaceGUID* {.header:headername,importc:"struct UnityInterfaceGUID"} = object 
      m_GUIDHigh*:culonglong
      m_GUIDLow*:culonglong
 
  IUnityInterface* {.header:headername, importc:"struct IUnityInterface".} = object
  
  IUnityInterfaces* {.header:headername, importc:"struct IUnityInterfaces".} = object 
      GetInterface* : proc (guid:UnityInterfaceGUID) : ptr IUnityInterface {.cdecl.} 
      RegisterInterface* : proc (guid:UnityInterfaceGUID, `ptr`:ptr IUnityInterface) {.cdecl.} 
      GetInterfaceSplit* : proc (guidHigh:uint64, guidLow:uint64) : ptr IUnityInterface {.cdecl.} 
      RegisterInterfaceSplit* : proc (guidHigh:uint64, guidLow:uint64, `ptr`:ptr IUnityInterface) {.cdecl.} 

type
    UnityTextureID* = cuint
