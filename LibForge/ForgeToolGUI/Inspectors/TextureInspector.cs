using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using LibForge;
using System.IO;

namespace ForgeToolGUI
{
  public partial class ImageInspector : Inspector
  {
    LibForge.Texture.Texture uncompressedTexture;
    public ImageInspector(LibForge.Texture.Texture i)
    {
      InitializeComponent();
      uncompressedTexture = i;
      Image decompressedImage = LibForge.Texture.TextureConverter.ToBitmap(i, 0, "default");
      pictureBox1.Width = decompressedImage.Width;
      pictureBox1.Height = decompressedImage.Height;
      pictureBox1.Image = decompressedImage;
    }

    private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
    {
      changeImageDecompression(DecompressList.SelectedItem.ToString());
    }

    private void changeImageDecompression(String decompression)
    {
      Image newDecompressedImage = LibForge.Texture.TextureConverter.ToBitmap(uncompressedTexture, 0, decompression);
      pictureBox1.Width = newDecompressedImage.Width;
      pictureBox1.Height = newDecompressedImage.Height;
      pictureBox1.Image = newDecompressedImage;
    }

    private void label1_Click(object sender, EventArgs e)
    {

    }

    private void button1_Click(object sender, EventArgs e)
    {
      SaveFileDialog dialog = new SaveFileDialog();
      dialog.InitialDirectory = @"./";
      dialog.RestoreDirectory = true;
      dialog.FileName = uncompressedTexture.FileName.Replace("bmp_ps4", "").Replace("png_ps4", "") + ".png";
      dialog.DefaultExt = "png";

      if(dialog.ShowDialog() == DialogResult.OK)
      {
        pictureBox1.Image.Save(dialog.FileName, System.Drawing.Imaging.ImageFormat.Png);
      }
    }
  }
}
