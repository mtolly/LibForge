using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GameArchives;
using GameArchives.STFS;
using DtxCS;
using DtxCS.DataTypes;
using LibForge;
using LibForge.Ark;
using LibForge.CSV;
using LibForge.Lipsync;
using LibForge.Mesh;
using LibForge.Midi;
using LibForge.Milo;
using LibForge.RBSong;
using LibForge.SongData;
using LibForge.Texture;
using LibForge.Util;

namespace ForgeToolGUI.Inspectors
{
  public partial class StartupInspector : Inspector
  {
    public StartupInspector()
    {
      InitializeComponent();
    }

    private void button1_Click(object sender, EventArgs e)
    {
      fb.OpenConverter();
    }

    private void openFileButton_Click(object sender, EventArgs e)
    {
      fb.openFile_Click(sender, e);
    }

    private void openPackageButton_Click(object sender, EventArgs e)
    {
      fb.openPackage_Click(sender, e);
    }

    private void openFolderButton_Click(object sender, EventArgs e)
    {
      fb.openFolder_Click(sender, e);
    }

    private void button1_Click_1(object sender, EventArgs e)
    {
    }

    private void button2_Click(object sender, EventArgs e)
    {
      fb.VRConverter();
    }
  }
}
