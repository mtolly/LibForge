using LibForge.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LibForge.Util
{
  public class BinWriter
  {
    protected bool BigEndian = false;
    protected Stream s;
    public BinWriter(Stream s)
    {
      this.s = s;
    }
    public void Write(byte v) => s.WriteByte(v);
    public void Write(short v)
    {
      if (BigEndian)
        s.WriteInt16BE(v);
      else
        s.WriteInt16LE(v);
    }
    public void Write(ushort v)
    {
      if (BigEndian)
        s.WriteUInt16BE(v);
      else
        s.WriteUInt16LE(v);
    }
    public void Write(int v)
    {
      if (BigEndian)
        s.WriteInt32BE(v);
      else
        s.WriteInt32LE(v);
    }
    public void Write(uint v)
    {
      if (BigEndian)
        s.WriteUInt32BE(v);
      else
        s.WriteUInt32LE(v);
    }
    public void Write(long v)
    {
      if (BigEndian)
        s.WriteInt64BE(v);
      else
        s.WriteInt64LE(v);
    }
    public void Write(ulong v)
    {
      if (BigEndian)
        s.WriteUInt64BE(v);
      else
        s.WriteUInt64LE(v);
    }
    public void Write(float v) => s.Write(BitConverter.GetBytes(v), 0, 4);
    public void Write(bool v) => s.WriteByte((byte)(v ? 1 : 0));
    public void Write(string v)
    {
      var bytes = Encoding.UTF8.GetBytes(v);
      
      if (BigEndian)
      {
        s.WriteInt32BE(bytes.Length);
      }
      else
      {
        s.WriteInt32LE(bytes.Length);
      }
      s.Write(bytes, 0, bytes.Length);
    }
    public void Write(string v, int length)
    {
      var bytes = Encoding.UTF8.GetBytes(v);
      s.Write(bytes, 0, bytes.Length);
      s.WriteByte(0);
      s.Position += length - bytes.Length - 1;
    }
    public void Write<T>(T[] arr, Action<T> writer)
    {
      // Treat uninitialized arrays as empty ones
      if (arr == null)
      {
        Write(0);
        return;
      }
      Write(arr.Length);
      foreach (var x in arr)
      {
        writer(x);
      }
    }
  }
}
