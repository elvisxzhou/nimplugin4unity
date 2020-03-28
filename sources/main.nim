include entry
import messagestream

type MessageHandler = proc(data:pointer, len:int)
var msgHandler:MessageHandler

proc SetMessageHandler(handler:MessageHandler) {.exportc, dynlib.} =
  msgHandler = handler

proc TriggerMessage(){.exportc, dynlib.} =
  var ms = newMessageStream()
  var writer = ms.beginWrite()
  discard writer.write_array("hello from plugin")
  msgHandler(ms.buffer, ms.length)

proc OnMessage(data: pointer, len: int) {.exportc, dynlib.} =
  TriggerMessage()

proc game_init*() {.exportc.} =
  discard

proc game_shutdown*() {.exportc.} =
  discard

proc OnUnityEvent(eventId:int) =
  discard

proc GetRenderEventFunc*() : pointer  {.exportc, dynlib.}  =
  return OnUnityEvent

