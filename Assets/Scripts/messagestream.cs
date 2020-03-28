using System;
using System.Runtime.InteropServices;

public class UnsafeUtility
{
  public static unsafe void MemCpy(void * dest, void* src, ulong count)
  {
    ulong block;

    block = count >> 3;

    ulong* pDest = (ulong*)dest;
    ulong* pSrc = (ulong*)src;

    for (ulong i = 0; i < block; i++)
    {
      *pDest = *pSrc; pDest++; pSrc++;
    }
    dest = pDest;
    src = pSrc;
    count = count - (block << 3);

    if (count > 0)
    {
      byte* pDestB = (byte*) dest;
      byte* pSrcB = (byte*) src;
      for (ulong i = 0; i < count; i++)
      {
        *pDestB = *pSrcB; pDestB++; pSrcB++;
      }
    }
  }
}

public class MessageStream : IDisposable
{
  const int ALLOC_SIZE = 1024;
  
  public MessageStream(IntPtr buf, int len)
  {
    buffer = buf;
    length = len;
    alloced = false;
  }

  public MessageStream()
  {
    buffer = Marshal.AllocHGlobal(ALLOC_SIZE);
    length = ALLOC_SIZE;
    alloced = true;
  }

  ~MessageStream()
  {
    Cleanup();
  }

  public IntPtr buffer;
  public int length;
  private bool alloced;
  
  public void print(int size)
  {
    //var len = Math.Min(size, length);
    //for (int i=0; i<len; ++i)
     // Console.WriteLine(buffer[i]);
  }

  public void Resize(int size)
  {
    if( size <= length ) return;

    length = (size/ALLOC_SIZE+1)*ALLOC_SIZE;

    if ( alloced )
      buffer = Marshal.ReAllocHGlobal(buffer, (IntPtr)length);
    else
    {
      alloced = true;
      buffer = Marshal.AllocHGlobal(length);
    }
  }

  public MessageStreamReader BeginRead()
  {
    var msr = new MessageStreamReader();
    msr.stream = this;
    msr.position = 0;
    return msr;
  }

  public MessageStreamWriter BeginWrite()
  {
    var msw = new MessageStreamWriter();
    msw.stream = this;
    msw.position = 0;
    return msw;
  }

  public void Dispose()
  {
    Cleanup();
    GC.SuppressFinalize(this);
  }

  void Cleanup()
  {
    if( alloced )
      Marshal.FreeHGlobal(buffer);
  }
}

unsafe public partial class MessageStreamReader
{
    public MessageStream stream;
    public int position;
    public bool advance(int count)
    {
      var newpos = position + count;
      if (newpos >= stream.length)
        return false;

      position = newpos;
      return true;
    }
    
    byte* cur_buffer { get { return (byte*)this.stream.buffer.ToPointer() + position; } }

    public int read_uint()
    {
      var buf = cur_buffer;
      var num = 0;
      var i = 0;
      while(true)
      {
        num += (buf[i]&0x7f) << (i*7);
        if((buf[i]&0x80) == 0)
          break;

        i++;
      }

      this.advance(i+1);

      return num;
    }

    public int read_sint()
    {
      var num = this.read_uint();
      return (num >> 1) ^ (-(num & 1));
    }

    public T read_fixed<T>() where T:unmanaged
    {
      var size = sizeof(T);
      var p = cur_buffer;
      //T val;
      //UnsafeUtility.MemCpy(&val, cur_buffer, (ulong)size);
      this.advance(size);
      //return val;
      return *((T*)p);
    }

    public string read_string()
    {
      var len = this.read_uint();
      Console.WriteLine("len = "+len);
      
      var str = new string((sbyte*)cur_buffer, 0, len);//, System.Text.Encoding.Unicode);
      for( int i=0; i<str.Length; ++i)
      {
        Console.WriteLine(str[i]);
      }

      this.advance(len);
      return str;
    }

    public void read_buf(void* buf, int len)
    {
      UnsafeUtility.MemCpy(buf, this.cur_buffer, (ulong)len);
      this.advance(len);
    }

    public T[] read_array<T>() where T:unmanaged
    {
      var len = this.read_uint();
      if(len > 0)
      {
          var result = new T[len/sizeof(T)];
          fixed( T* p = result )
            read_buf(p, len);
          return result;
      }

      return null;
    }
}

unsafe public partial class MessageStreamWriter
{
    public MessageStream stream;
    public int position;

    public bool advance(int count)
    {
      var newpos = this.position + count;
      if (newpos >= this.stream.length)
      {
        this.stream.Resize(newpos);
      }

      this.position = newpos;
      return true;
    }

    byte* cur_buffer { get { return (byte*)this.stream.buffer.ToPointer() + position; } }

    public int write_uint(long val)
    {
      var buf = this.cur_buffer;
      var num = val;
      var i = 0;
      while (num > 0)
      {
        var m = num & 0x7f;
        num = num >> 7;
        buf[i] = (byte)m;
        if( num > 0 )
          buf[i] = (byte)(buf[i] | 0x80);

        i++;
      }

      this.advance(i);
      return i;
    }

    public int write_sint(long val)
    {
      var num = (val << 1) ^ (val >> 63);
      return this.write_uint(num);
    }

    public int write_fixed<T>(T val) where T:unmanaged
    {
      var size = sizeof(T);
      var p = cur_buffer;
      //UnsafeUtility.MemCpy(cur_buffer, &val, (ulong)size);
      *((T*)p) = val;
      this.advance(size);
      return size;
    }

    public int write_string(string val)
    {
      fixed(void* p = val)
        return write_buf(p, val.Length*sizeof(Char));
    }

    public int write_buf(void* buf, int len)
    {
        var sz = this.write_uint(len);
        UnsafeUtility.MemCpy(cur_buffer, buf, (ulong)len);
        this.advance(len);
        return len;
    }

    public int write_array<T>(T[] val) where T:unmanaged
    {
        fixed( void* p = val )
        {
          return write_buf(p, val.Length*sizeof(T));
        }
    }
}

/*
public class Helloworld
{
  public static void Main(string[] args)
  {
    var ms = new MessageStream();
    var writer = ms.BeginWrite();
    var size = writer.write_uint(123);
    Console.WriteLine(size);
    size = writer.write_sint(63);
    Console.WriteLine(size);
    size = writer.write_sint(64);
    Console.WriteLine(size);
    size = writer.write_fixed<float>(1234.023f);
    Console.WriteLine(size);
    size = writer.write_string("asdfiowdsf");
    Console.WriteLine(size);
    size = writer.write_array<float>(new float[]{1234.023f,23.0f,23.111f,2355.0f});
    Console.WriteLine(size);

    var reader = ms.BeginRead();
    var i = reader.read_uint();
    Console.WriteLine(i);
    i = reader.read_sint();
    Console.WriteLine(i);
    i = reader.read_sint();
    Console.WriteLine(i);
    var j = reader.read_fixed<float>();
    Console.WriteLine(j);
    var s = reader.read_string();
    Console.WriteLine(s);
    var k = reader.read_array<float>();
    foreach( var v in k )
      Console.WriteLine(v);
  }
} 
*/
