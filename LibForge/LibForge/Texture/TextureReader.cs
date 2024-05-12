using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using LibForge.Util;
using System.IO.Compression;

namespace LibForge.Texture
{
  public class TextureReader : ReaderBase<Texture>
  {
    public static Texture ReadStream(Stream s)
    {
      return new TextureReader(s).Read();
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
      var hdrData = magic == 6 ? FixedArr(Byte, version == 0xC ? 0x7Cu : 0xACu) : FixedArr(Byte, 0xA8);
      var MipmapLevels = UInt();
      var Mipmaps = FixedArr(() => new Texture.Mipmap
      {
        Width = Int(),
        Height = Int(),
        Flags = Int(),
        DecompressedSize = version == 0xC ? Int() : 0,
        CompressionFlags = version == 0xC ? Int() : 0,
      }, MipmapLevels);
      UInt();
      for(var i = 0; i < Mipmaps.Length; i++)
      {
        Mipmaps[i].Data = Arr(Byte);
        // If there's a decompressed size we should zlib decompress the data
        if (Mipmaps[i].DecompressedSize > 0)
        {
          byte[] decompressed = new byte[Mipmaps[i].DecompressedSize];
          using (MemoryStream ms = new MemoryStream(Mipmaps[i].Data))
          using (DeflateStream deflate = new DeflateStream(ms, CompressionMode.Decompress))
          {
            ms.Read(decompressed, 0, 2); // skip over zlib header
            deflate.Read(decompressed, 0, Mipmaps[i].DecompressedSize);
          }
          Mipmaps[i].Data = decompressed;
        }
        // DDS textures (png_xb1/bmp_xb1) only have 1 blob of data for mipmaps
        if (Mipmaps[i].CompressionFlags != 0)
          break;
      }
      var footerData = FixedArr(Byte, 0x1C);
      return new Texture
      {
        Version = magic,
        Mipmaps = Mipmaps,
        HeaderData = hdrData,
        FooterData = footerData
      };
    }
  }
}
