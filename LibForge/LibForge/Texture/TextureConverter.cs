using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using LibForge.Extensions;

namespace LibForge.Texture
{
  public class TextureConverter
  {
    static int RGB565ToARGB(ushort input)
    {
      return Color.FromArgb(0xFF,
        ((input >> 11) & 0x1F) << 3 | ((input >> 11) & 0x1F) >> 2,
        ((input >> 5) & 0x3F) << 2 | ((input >> 5) & 0x3F) >> 4,
        (input & 0x1F) << 3 | (input & 0x1F) >> 2).ToArgb();
    }
    static ushort ARGBToRGB565(Color input)
    {
      return (ushort)
        ((((input.R * 0x1F / 0xFF) & 0x1F) << 11) |
         (((input.G * 0x3F / 0xFF) & 0x3F) << 5) |
         (((input.B * 0x1F / 0xFF) & 0x1F)));
    }
    // TODO: Decode DXT5 alpha channel
    public static Bitmap ToBitmap(Texture t, int mipmap, string compressionType)
    {
      var m = t.Mipmaps[mipmap];
      var output = new Bitmap(m.Width, m.Height, PixelFormat.Format32bppArgb);
      int[] imageData = new int[m.Width * m.Height];
      if (m.Data.Length == (imageData.Length * 4))
      {
        Buffer.BlockCopy(m.Data, 0, imageData, 0, m.Data.Length);
      } else if (compressionType.ToUpper() == "R8G8")
      {
        DecodeR8G8(m, imageData);
      } else if (compressionType.ToUpper() == "BC4")
      {
        DecodeBC4(m, imageData);
      } else if (compressionType.ToUpper() == "BC5")
      {
        DecodeBC5(m, imageData);
      } else if (compressionType.ToUpper() == "DXT" || compressionType.ToLower() == "default" || compressionType.ToUpper() == "BC3")
      {
        if (m.Data.Length == imageData.Length)
        {
          DecodeDXT(m, imageData, true, compressionType);
        }
        else if (m.Data.Length == (imageData.Length / 2))
        {
          DecodeDXT(m, imageData, false, compressionType);
        } else
        {
          throw new Exception($"Don't know what to do with this texture (version={t.Version})... Hint: Try R8G8 compressionType");
        }
      }
      else
      {
        throw new Exception("Argument compressionType missing!");
      }
      // Copy data to bitmap
      {
        var data = output.LockBits(new Rectangle(0, 0, m.Width, m.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        System.Runtime.InteropServices.Marshal.Copy(imageData, 0, data.Scan0, imageData.Length);
        output.UnlockBits(data);
      }
      return output;
    }

    // (BC4 Block ATI1/3Dc+)
    private static void DecodeBC4(Texture.Mipmap m, int[] imageData)
    {
      using (var s = new MemoryStream(m.Data))
      {
        for (var y = 0; y < m.Height; y += 4)
        {
          for (var x = 0; x < m.Width; x += 4)
          {
            float[] red = new float[16];

            byte[] redColorData = s.ReadBytes(8);
            ushort red0 = redColorData[0];
            ushort red1 = redColorData[1];
            red[0] = red0;
            red[1] = red1;
            ulong redMask = redColorData[2] | ((ulong)redColorData[3] << 8) | ((ulong)redColorData[4] << 16) | ((ulong)redColorData[5] << 24) | ((ulong)redColorData[6] << 32) | ((ulong)redColorData[7] << 40);

            if (red0 > red1)
            {
              // 6 interpolated color values
              red[2] = (6 * red0 + 1 * red1) / 7.0f; // bit code 010
              red[3] = (5 * red0 + 2 * red1) / 7.0f; // bit code 011
              red[4] = (4 * red0 + 3 * red1) / 7.0f; // bit code 100
              red[5] = (3 * red0 + 4 * red1) / 7.0f; // bit code 101
              red[6] = (2 * red0 + 5 * red1) / 7.0f; // bit code 110
              red[7] = (1 * red0 + 6 * red1) / 7.0f; // bit code 111
            }
            else
            {
              // 4 interpolated color values
              red[2] = (4 * red0 + 1 * red1) / 5.0f; // bit code 010
              red[3] = (3 * red0 + 2 * red1) / 5.0f; // bit code 011
              red[4] = (2 * red0 + 3 * red1) / 5.0f; // bit code 100
              red[5] = (1 * red0 + 4 * red1) / 5.0f; // bit code 101
              red[6] = 0.0f;                     // bit code 110
              red[7] = 1.0f;                     // bit code 111
            }

            var offset = y * m.Width + x;
            int currentIndex = 0;
            for (var i = 0; i < 4; i++)
            {
              for (var j = 0; j < 4; j++)
              {
                int redIndex = (int)((redMask >> (3 * currentIndex)) & 0x7);
                var selectedRed = red[redIndex];


                var color = Color.FromArgb(0XFF, (int)selectedRed, (int)selectedRed, (int)selectedRed).ToArgb();
                imageData[offset + i * m.Width + j] = color;
                currentIndex++;
              }
            }
          }
        }
      }
    }

    // usually used for normal maps (BC5 Block ATI2/3Dc)
    private static void DecodeBC5(Texture.Mipmap m, int[] imageData) {

      using (var s = new MemoryStream(m.Data))
      {
        for (var y = 0; y < m.Height; y += 4)
        {
          for (var x = 0; x < m.Width; x += 4)
          {
            float[] red = new float[16];
            float[] green = new float[16];

            byte[] redColorData = s.ReadBytes(8);
            ushort red0 = redColorData[0];
            ushort red1 = redColorData[1];
            red[0] = red0;
            red[1] = red1;
            ulong redMask = redColorData[2] | ((ulong)redColorData[3] << 8) | ((ulong)redColorData[4] << 16) | ((ulong)redColorData[5] << 24) | ((ulong)redColorData[6] << 32) | ((ulong)redColorData[7] << 40);

            byte[] greenColorData = s.ReadBytes(8);
            ushort green0 = greenColorData[0];
            ushort green1 = greenColorData[1];
            green[0] = green0;
            green[1] = green1;
            ulong greenMask = greenColorData[2] | ((ulong)greenColorData[3] << 8) | ((ulong)greenColorData[4] << 16) | ((ulong)greenColorData[5] << 24) | ((ulong)greenColorData[6] << 32) | ((ulong)greenColorData[7] << 40);

            if (red0 > red1)
            {
              // 6 interpolated color values
              red[2] = (6 * red0 + 1 * red1) / 7.0f; // bit code 010
              red[3] = (5 * red0 + 2 * red1) / 7.0f; // bit code 011
              red[4] = (4 * red0 + 3 * red1) / 7.0f; // bit code 100
              red[5] = (3 * red0 + 4 * red1) / 7.0f; // bit code 101
              red[6] = (2 * red0 + 5 * red1) / 7.0f; // bit code 110
              red[7] = (1 * red0 + 6 * red1) / 7.0f; // bit code 111
            }
            else
            {
              // 4 interpolated color values
              red[2] = (4 * red0 + 1 * red1) / 5.0f; // bit code 010
              red[3] = (3 * red0 + 2 * red1) / 5.0f; // bit code 011
              red[4] = (2 * red0 + 3 * red1) / 5.0f; // bit code 100
              red[5] = (1 * red0 + 4 * red1) / 5.0f; // bit code 101
              red[6] = 0.0f;                     // bit code 110
              red[7] = 1.0f;                     // bit code 111
            }

            if (green0 > green1)
            {
              // 6 interpolated color values
              green[2] = (6 * green0 + 1 * green1) / 7.0f; // bit code 010
              green[3] = (5 * green0 + 2 * green1) / 7.0f; // bit code 011
              green[4] = (4 * green0 + 3 * green1) / 7.0f; // bit code 100
              green[5] = (3 * green0 + 4 * green1) / 7.0f; // bit code 101
              green[6] = (2 * green0 + 5 * green1) / 7.0f; // bit code 110
              green[7] = (1 * green0 + 6 * green1) / 7.0f; // bit code 111
            }
            else
            {
              // 4 interpolated color values
              green[2] = (4 * green0 + 1 * green1) / 5.0f; // bit code 010
              green[3] = (3 * green0 + 2 * green1) / 5.0f; // bit code 011
              green[4] = (2 * green0 + 3 * green1) / 5.0f; // bit code 100
              green[5] = (1 * green0 + 4 * green1) / 5.0f; // bit code 101
              green[6] = 0.0f;                     // bit code 110
              green[7] = 1.0f;                     // bit code 111
            }


            var offset = y * m.Width + x;
            int currentIndex = 0;
            for (var i = 0; i < 4; i++)
            {
              for (var j = 0; j < 4; j++)
              {
                int redIndex = (int)((redMask >> (3 * currentIndex)) & 0x7);
                var selectedRed = red[redIndex];
                int greenIndex = (int)((greenMask >> (3 * currentIndex)) & 0x7);
                var selectedGreen = green[greenIndex];

                // not sure about calculating channel blue
                // var computedBlue = Math.Sqrt(1.0 - (Math.Pow(selectedRed / 1000, 2)) - (Math.Pow(selectedGreen / 1000, 2))) * 255;

                var color = Color.FromArgb(0XFF, (int)selectedRed, (int)selectedGreen, 0xFF).ToArgb();
                imageData[offset + i * m.Width + j] = color;
                currentIndex++;
              }
            }
          }
        }
      }
    }

    // seems like this is used for uncompressed normal maps
    // not sure about calculating blue
    private static void DecodeR8G8(Texture.Mipmap m, int[] imageData)
    {

      using (var s = new MemoryStream(m.Data))
      {
        for (var i = 0; i < imageData.Length; i++)
        {
          ushort red = s.ReadUInt8();
          ushort green = s.ReadUInt8();
          //var computedBlue = Math.Sqrt(1.0 - (Math.Pow(red / 1000, 2)) - (Math.Pow(green / 1000, 2))) * 255;

          var color = Color.FromArgb(0xFF, red, green, 0xFF).ToArgb();
          imageData[i] = color;
        }
      }
    }


    private static void DecodeDXT(Texture.Mipmap m, int[] imageData, bool DXT5, string compressionType)
    {
      int[] alpha = new int[16];
      int[] colors = new int[4];
      using (var s = new MemoryStream(m.Data))
      {
        byte[] iData = new byte[4];

        for (var y = 0; y < m.Height; y += 4)
          for (var x = 0; x < m.Width; x += 4)
              {
                byte[] alphaData;
                ushort alpha0;
                ushort alpha1;
                ulong alphaMask = 0;
                if (DXT5)
                {
                  alphaData = s.ReadBytes(8);
                  alpha0 = alphaData[0];
                  alpha1 = alphaData[1];
                  alphaMask = alphaData[2] | ((ulong)alphaData[3] << 8) |
                  ((ulong)alphaData[4] << 16) | ((ulong)alphaData[5] << 24) |
                  ((ulong)alphaData[6] << 32) | ((ulong)alphaData[7] << 40);


                  alpha[0] = alpha0;
                  alpha[1] = alpha1;

                  if (alpha0 > alpha1)
                  {
                    alpha[2] = (byte)((6 * alpha0 + alpha1) / 7);
                    alpha[3] = (byte)((5 * alpha0 + 2 * alpha1) / 7);
                    alpha[4] = (byte)((4 * alpha0 + 3 * alpha1) / 7);
                    alpha[5] = (byte)((3 * alpha0 + 4 * alpha1) / 7);
                    alpha[6] = (byte)((2 * alpha0 + 5 * alpha1) / 7);
                    alpha[7] = (byte)((alpha0 + 6 * alpha1) / 7);
                  }

                  else
                  {
                    alpha[2] = (byte)((4 * alpha0 + alpha1) / 5);
                    alpha[3] = (byte)((3 * alpha0 + 2 * alpha1) / 5);
                    alpha[4] = (byte)((2 * alpha0 + 3 * alpha1) / 5);
                    alpha[5] = (byte)((alpha0 + 4 * alpha1) / 5);
                    alpha[6] = 0;
                    alpha[7] = 255;
                  }

                }

                ushort c0 = s.ReadUInt16LE();
                ushort c1 = s.ReadUInt16LE();
                colors[0] = RGB565ToARGB(c0);
                colors[1] = RGB565ToARGB(c1);
                var color0 = Color.FromArgb(colors[0]);
                var color1 = Color.FromArgb(colors[1]);
                s.Read(iData, 0, 4);

                ulong colorMask = ((ulong)iData[0]) | ((ulong)iData[1] << 8) | ((ulong)iData[2] << 16) | ((ulong)iData[3] << 24);

                if (c0 > c1)
                {
                  colors[2] = Color.FromArgb(0xFF,
                    (color0.R * 2 + color1.R) / 3,
                    (color0.G * 2 + color1.G) / 3,
                    (color0.B * 2 + color1.B) / 3).ToArgb();
                  colors[3] = Color.FromArgb(0xFF,
                    (color0.R + (color1.R * 2)) / 3,
                    (color0.G + (color1.G * 2)) / 3,
                    (color0.B + (color1.B * 2)) / 3).ToArgb();
                }
                else
                {
                  colors[2] = Color.FromArgb(0xFF,
                    (color0.R + color1.R) / 2,
                    (color0.G + color1.G) / 2,
                    (color0.B + color1.B) / 2).ToArgb();
                  colors[3] = Color.Black.ToArgb();
                }
                var offset = y * m.Width + x;
                int currentIndex = 0;
                for (var i = 0; i < 4; i++)
                {
                  for (var j = 0; j < 4; j++)
                  {
                    if (DXT5)
                    {
                      var idx = (colorMask >> (2 * currentIndex)) & 0x3;
                      // var idx = (iData[i] >> (2 * j)) & 0x3;
                      var selectedColor = colors[idx];
                      int alphaIndex = (int)((alphaMask >> (3 * currentIndex)) & 0x7);
                      var selectedAlpha = alpha[alphaIndex];
                      var colorWithoutAlpha = Color.FromArgb(selectedColor);
                      var colorWithAlpha = Color.FromArgb(selectedAlpha, colorWithoutAlpha).ToArgb();
                      imageData[offset + i * m.Width + j] = colorWithAlpha;
                    }
                    else
                    {
                      var idx = (iData[i] >> (2 * j)) & 0x3;
                      imageData[offset + i * m.Width + j] = colors[idx];
                    }
                    currentIndex++;
                  }
                }
            }
      }
    }

    private static double ColorDist(Color c1, Color c2)
    {
      // According to Wikipedia, these coefficients should produce an OK delta
      return Math.Sqrt(
          2 * Math.Pow(c1.R - c2.R, 2) 
        + 4 * Math.Pow(c1.G - c2.G, 2) 
        + 3 * Math.Pow(c1.B - c2.B, 2));
    }


    private static IEnumerable<Color> EnumerateBlockColors(Bitmap img, int x, int y)
    {
      for (var y0 = 0; y0< 4; y0++)
        for (var x0 = 0; x0< 4; x0++)
          yield return img.GetPixel(x + x0, y + y0);
    }

    private static byte[] EncodeDxt(Image image, int mapLevel, int nominalSize = 256)
    {
      var img = new Bitmap(image, new Size(nominalSize >> mapLevel, nominalSize >> mapLevel));
      var data = new byte[img.Width * img.Height / 2];
      var idx = 0;
      for(var y = 0; y < img.Height; y += 4)
        for (var x = 0; x < img.Width; x += 4)
        {
          // Pick the farthest-apart colors in this block as the endpoints
          int i0 = 0, j0 = 1;
          var blockColors = EnumerateBlockColors(img, x, y).ToArray();
          double highest = 0;
          for (var i = 0; i < 16; i++)
          {
            for(var j = i + 1; j < 16; j++)
            {
              var d = ColorDist(blockColors[i], blockColors[j]);
              if (d >= highest)
              {
                i0 = i;
                j0 = j;
                highest = d;
              }
            }
          }
          var c1 = blockColors[i0];
          var c2 = blockColors[j0];
          var colors = new[]
          {
            c1, c2,
            Color.FromArgb(0xFF,
              (c1.R * 2 + c2.R) / 3,
              (c1.G * 2 + c2.G) / 3,
              (c1.B * 2 + c2.B) / 3),
            Color.FromArgb(0xFF,
              (c1.R + (c2.R * 2)) / 3,
              (c1.G + (c2.G * 2)) / 3,
              (c1.B + (c2.B * 2)) / 3)
          };
          var color0 = ARGBToRGB565(colors[0]);
          var color1 = ARGBToRGB565(colors[1]);
          Color tmp;
          
          if (color0 < color1)
          {
            // swap colors
            color0 ^= color1;
            color1 ^= color0;
            color0 ^= color1;
            tmp = colors[0];
            colors[0] = colors[1];
            colors[1] = tmp;
          }
          if(color0 == color1)
          {
            // The square is uniform, so just tell the later code not to use color3
            colors[3] = Color.Black;
          }
          data[idx++] = (byte)(color0 & 0xFF);
          data[idx++] = (byte)(color0 >> 8);
          data[idx++] = (byte)(color1 & 0xFF);
          data[idx++] = (byte)(color1 >> 8);

          for (var j = 0; j < 4; j++, idx++)
          {
            for (var i = 0; i < 4; i++)
            {
              var pixel = blockColors[i + 4 * j];
              double lowest = double.MaxValue;
              int bestColor = 0;
              for (var k = 0; k < colors.Length; k++)
              {
                var diff = ColorDist(colors[k], pixel);
                if (diff < lowest)
                {
                  lowest = diff;
                  bestColor = k;
                }
              }
              data[idx] |= (byte)(bestColor << (i * 2));
            }
          }
        }
      return data;
    }

    static byte[] HeaderData256x256 = new byte[]
    {
      0x09, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x10, 0x00, 0x00, 0x00,
      0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
      0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x20, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
      0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
      0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
      0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x06, 0x00, 0x00, 0x00,
      0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00,
      0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x04, 0x00, 0x00, 0x00,
      0x02, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00,
      0x00, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
      0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
    };

    static byte[] FooterData256x256 = new byte[]
    {
      0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x00, 0x00, 0x0F, 0xC2, 0x73, 0x3D, 0x1C, 0x99, 0x4B, 0x3D,
      0x05, 0xC1, 0x0D, 0x3E, 0x00, 0x00, 0x80, 0x3F, 0x08, 0x00, 0x00, 0x00
    };

    public static Texture MiloPngToTexture(Stream s)
    {
      var version = s.ReadUInt24BE();
      if (version != 0x010818 && version != 0x010408)
        throw new ArgumentException("Stream was not a supported png_xbox");
      s.Position += 4;
      var width = s.ReadInt16LE();
      var height = s.ReadInt16LE();
      s.Position = 32;
      if (width == 1024)
      {
        s.Position += (1024 * 1024) / (version == 0x010818 ? 1 : 2);
        width = 512;
        height = 512;
      }
      if(width == 512)
      {
        s.Position += (512 * 512) / (version == 0x010818 ? 1 : 2);
        width = 256;
        height = 256;
      }
      if(width != 256 || height != 256)
      {
        throw new Exception("Texture was not 512x512 or 256x256");
      }
      var mipmaps = new List<Texture.Mipmap>();
      for(var i = 0; i < 7; i++)
      {
        var m = new Texture.Mipmap
        {
          Width = width,
          Height = height,
          Data = new byte[width * height / 2]
        };
        var bytes = s.ReadBytes(width * height / (version == 0x010818 ? 1 : 2));
        if(bytes.Length == 0)
        {
          break;
        }
        for (int x = (version == 0x010818 ? 0 : -8), y = 0; y < m.Data.Length; x += (version == 0x010818 ? 16 : 8))
        {
          m.Data[y] = bytes[x + 9];
          m.Data[y + 1] = bytes[x + 8];
          m.Data[y + 2] = bytes[x + 11];
          m.Data[y + 3] = bytes[x + 10];
          m.Data[y + 4] = bytes[x + 13];
          m.Data[y + 5] = bytes[x + 12];
          m.Data[y + 6] = bytes[x + 15];
          m.Data[y + 7] = bytes[x + 14];
          y += 8;
        }
        width /= 2;
        height /= 2;
        mipmaps.Add(m);
      }
      return new Texture
      {
        HeaderData = HeaderData256x256,
        FooterData = FooterData256x256,
        Version = 6,
        Mipmaps = mipmaps.ToArray()
      };
    }

    public static Texture ToTexture(Image image)
    {
      Texture.Mipmap[] maps = new Texture.Mipmap[7];
      for(var i = 0; i < maps.Length; i++)
      {
        maps[i] = new Texture.Mipmap
        {
          Width = 256 / (1 << i),
          Height = 256 / (1 << i),
          Data = EncodeDxt(image, i)
        };
      }
      return new Texture
      {
        HeaderData = HeaderData256x256,
        FooterData = FooterData256x256,
        Version = 6,
        Mipmaps = maps
      };
    }
  }
}
