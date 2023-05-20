using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using LibForge.Util;

namespace LibForge.Texture
{
  public class TextureReader : ReaderBase<Texture>
  {
    public static Texture ReadStream(Stream s, string fileName = "")
    {
      var tr = new TextureReader(s).Read();
      tr.FileName = fileName;
      return tr;
    }
    public TextureReader(Stream s) : base(s) { }

    public override Texture Read()
    {
      var magic = Int();
      if(magic != 6 && magic != 4)
      {
        throw new Exception($"Unknown texture magic {magic}");
      }
      var version = Int();
      long position = s.Position;
      //Console.WriteLine(s.Position.ToString());
      var hdrData = magic == 6 ? FixedArr(Byte, version == 0xC ? 0x7Cu : 0xACu) : FixedArr(Byte, 0xA4);
      /* Console.WriteLine(s.Position.ToString());
      Console.WriteLine("[{0}]", string.Join(", ", hdrData)); */
      var MipmapLevels = UInt();
      var Mipmaps = FixedArr(() => new Texture.Mipmap
      {
        Width = Int(),
        Height = Int(),
        Flags = version == 0xC ? Int().Then(Skip(8)) : Int()
      }, MipmapLevels);
      UInt();
      for(var i = 0; i < Mipmaps.Length; i++)
      {
        Mipmaps[i].Data = Arr(Byte);
      }
      var numberOfPixels = Mipmaps[0].Width * Mipmaps[0].Height;
      var dataLength = Mipmaps[0].Data.Length;
      Console.WriteLine("[{0}]", string.Join(", ", hdrData));
      float result = (float)numberOfPixels / (float)dataLength;
      /* Console.WriteLine(numberOfPixels.ToString() + "px" + " - " + dataLength.ToString() + "byte "  + "result: " + result);
      Console.WriteLine(version);
      Console.WriteLine("---------------"); */
      var footerData = FixedArr(Byte, 0x1C);
      return new Texture
      {
        Version = magic,
        Mipmaps = Mipmaps,
        HeaderData = hdrData,
        FooterData = footerData,
      };
    }
  }
}
