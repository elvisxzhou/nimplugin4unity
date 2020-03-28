import strutils

const
  ALLOC_SIZE = 1024

type
  MessageStreamObj* = object
    buffer*   : ptr UncheckedArray[uint8]
    length*   : int

  MessageStream* = ref MessageStreamObj

  MessageStreamReader* = object
    stream*:ref MessageStreamObj
    position*:int

  MessageStreamWriter* = object
    stream*:ref MessageStreamObj
    position*:int

proc destroyMessageStream*(this:MessageStream)=
  if this.buffer != nil:
    dealloc(this.buffer)
    this.buffer = nil

proc finalizer(this:MessageStream)=
  this.destroyMessageStream()

proc newMessageStream*():MessageStream=
  new(result,finalizer)
  result.buffer = cast[result.buffer.type](alloc(ALLOC_SIZE))
  result.length = ALLOC_SIZE

proc print*(this:MessageStream, size:int)=
  var len = min(size, this.length)
  for i in 0..<len:
    echo this.buffer[i].int.toBin(8)

#[
proc `=destroy`*(this:var MessageStreamObj)=
  if this.buffer != nil:
    dealloc(this.buffer)
    this.buffer = nil

proc `=`* (a:var MessageStream, b:MessageStream)=
  echo "assignment invoked $1 = $2" % [a.repr, b.repr]
  if a.buffer == b.buffer: return
  `=destroy`(a)
  a.position = b.position
  a.length = b.length
  if b.buffer != nil:
    a.buffer = alloc(b.length)
    copyMem(a.buffer, b.buffer, b.length)

proc `=sink`*(a:var MessageStream, b:MessageStream)=
  echo "sink assignment invoked $1 = $2" % [a.repr, b.repr]
  `=destroy`(a)
  a.buffer = b.buffer
  a.position = b.position
  a.length = b.length

]#

proc advance(this:var MessageStreamReader, count:int):bool =
  var newpos = this.position + count
  if newpos >= this.stream.length:
    return false

  this.position = newpos
  return true

proc advance(this:var MessageStreamWriter, count:int):bool =
  var newpos = this.position + count
  if newpos >= this.stream.length:
    var newsize = (newpos div ALLOC_SIZE)*ALLOC_SIZE+ALLOC_SIZE
    var newbuffer = cast[this.stream.buffer.type](realloc(this.stream.buffer, newsize))
    this.stream.buffer = newbuffer
    this.stream.length = newsize

  this.position = newpos
  return true

proc beginRead*(this:MessageStream):MessageStreamReader = 
  result.stream = this
  result.position = 0

proc beginWrite*(this:MessageStream):MessageStreamWriter = 
  result.stream = this
  result.position = 0

#template reset(this:MessageStreamReader|MessageStreamWriter) =
  #this.position = 0

template cur_buffer(this):ptr UncheckedArray[uint8] =
  cast[ptr UncheckedArray[uint8]](this.stream.buffer[this.position].unsafeAddr) #cast[pointer](cast[int](this.stream.buffer) + this.position)

proc read_uint*(this:var MessageStreamReader) : int =
  var buf = this.cur_buffer
  var num = 0
  var i = 0
  while true:
    inc(num,  (buf[i].int and 0x7f ) shl (i*7))
    if (buf[i].int and 0x80) == 0:
      break

    inc(i)

  doAssert this.advance(i+1)

  return num

proc read_sint*(this:var MessageStreamReader) : int =
  var num = this.read_uint()
  return (num shr 1).int xor -(num and 1).int

proc read_fixed*(this:var MessageStreamReader, t:typedesc[SomeOrdinal|SomeFloat]) : auto =
  var res:t
  var buf = this.cur_buffer
  copyMem(res.unsafeAddr, buf, t.sizeof)
  doAssert this.advance(t.sizeof)
  return res

proc read_array*[T:char|SomeOrdinal|SomeFloat](this:var MessageStreamReader, t:typedesc[openArray[T]]) : t =
  var len = this.read_uint()
  if len > 0:
    result.setLen((len/T.sizeof).int)
    copyMem(result[0].unsafeAddr, this.cur_buffer, len)
    doAssert this.advance(len)

proc write_uint*(this:var MessageStreamWriter, val:int) : int =
  var buf = this.cur_buffer
  var num = val
  var i = 0;
  while num > 0:
    var m = num and 0x7f
    num = num shr 7
    buf[i] = m.uint8
    if num > 0:
      buf[i] = buf[i] or 0x80
    inc(i)

  doAssert this.advance(i)
  return i

proc write_sint*(this:var MessageStreamWriter, val:int) : int =
  var num = (val shl 1) xor (val shr 63)
  return this.write_uint(num)

proc write_fixed*[T:SomeOrdinal|SomeFloat](this:var MessageStreamWriter, val:T) : int =
  copyMem(this.cur_buffer, val.unsafeAddr, T.sizeof)
  doAssert this.advance(T.sizeof)
  return T.sizeof

proc write_array*[T:char|SomeOrdinal|SomeFloat](this:var MessageStreamWriter, val:openArray[T]) : int =
  var len = val.len*T.sizeof
  var sz = this.write_uint(len)

  if len > 0:
    var buf = this.cur_buffer
    copyMem(buf, val[0].unsafeAddr, len)
    doAssert this.advance(len)

  return len + sz


when isMainModule:

  var ms = newMessageStream()
  var writer = ms.beginWrite()
  doAssert( writer.write_fixed(123.4.float32) == 4 )
  doAssert( writer.write_fixed(-10.int8) == 1 )
  doAssert( writer.write_fixed(10.int16) == 2 )
  doAssert( writer.write_fixed(-110.int32) == 4 )
  doAssert( writer.write_fixed(10.int64) == 8 )
  doAssert( writer.write_sint(-64) == 1 )
  doAssert( writer.write_sint(-65) == 2 )
  doAssert( writer.write_sint(63) == 1 )
  doAssert( writer.write_sint(64) == 2 )
  doAssert( writer.write_array("hello world") == 12 )
  doAssert( writer.write_array(@[1.0f,2,3,4,5]) == 21 )

  var reader = ms.beginRead()
  echo reader.read_fixed(float32)
  echo reader.read_fixed(int8)
  echo reader.read_fixed(int16)
  echo reader.read_fixed(int32)
  echo reader.read_fixed(int64)
  echo reader.read_sint()
  echo reader.read_sint()
  echo reader.read_sint()
  echo reader.read_sint()
  echo reader.read_array(string)
  echo reader.read_array(seq[float32])

