using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GameArchives.STFS;
using LibForge.Util;
using System.Text.RegularExpressions;
using System.IO;
using DtxCS;
using GameArchives;
using LibForge;
using LibForge.Ark;
using LibForge.Midi;
using LibForge.RBSong;
using LibForge.SongData;
using LibForge.Texture;

namespace ForgeToolGUI.Inspectors
{
  public partial class VRConversionInspector : Inspector
  {
    public VRConversionInspector()
    {
      InitializeComponent();
    }

    void ClearState()
    {
      listBox1.Items.Clear();
      dtas.Clear();
      UpdateState();
    }
    void UpdateState()
    {
      if (dtas.Count == 0)
      {
        groupBox2.Enabled = false;
        groupBox3.Enabled = false;
        prefixBox.Text = "";
        return;
      }
      var dtaList = dtas.Values.SelectMany(x => x).ToList();
      dtaList.Sort((a, b) => a.Shortname.CompareTo(b.Shortname));
      groupBox2.Enabled = true;
    }

    private void pickFileButton_Click(object sender, EventArgs e)
    {
      using (var ofd = new OpenFileDialog() { Multiselect = true })
      {
        if(ofd.ShowDialog() == DialogResult.OK)
        {
          LoadCons(ofd.FileNames);
        }
      }
    }

    Dictionary<string, List<LibForge.SongData.SongData>> dtas = new Dictionary<string, List<LibForge.SongData.SongData>>();
    private bool LoadCon(string filename)
    {
      using (var con = STFSPackage.OpenFile(GameArchives.Util.LocalFile(filename)))
      {
        if (con.Type != STFSType.CON)
        {
          throw new Exception($"File is not a CON file.");
        }
        var datas = PkgCreator.GetSongMetadatas(con.RootDirectory.GetDirectory("songs"));
        if (datas.Count > 0)
        {
          dtas[filename] = datas;
          listBox1.Items.Add(filename);
        }
      }
      return true;
    }
    private void LoadCons(string[] filenames)
    {
      foreach (var filename in filenames)
      {
        try
        {
          LoadCon(filename);
        }
        catch (Exception e)
        {
          logBox.AppendText($"Error loading {filename}: {e.Message}" + Environment.NewLine);
        }
      }
      UpdateState();
    }
    void RemoveCon(string filename)
    {
      listBox1.Items.Remove(filename);
      dtas.Remove(filename);
    }

    private void idBox_TextChanged(object sender, EventArgs e)
    {
      
    }

    private void euCheckBox_CheckedChanged(object sender, EventArgs e)
    {
      
    }

    private void buildButton_Click(object sender, EventArgs e)
    {
      Action<string> log = x => logBox.AppendText(x + Environment.NewLine);

      var prefix = prefixBox.Text;
      var rbvrPath = folderName.Text;
      var tempDir = Path.Combine(Path.GetTempPath(), "forgetool_tmp_build");
      if (!rawCheckBox.Checked)
      {
        log("converting CONs to ARKs...");
        var cons = listBox1.Items.OfType<string>().Select(f => STFSPackage.OpenFile(GameArchives.Util.LocalFile(f))).ToList();
        foreach (var conFile in cons)
        {
          var songs = PkgCreator.ConvertDLCPackage(conFile.RootDirectory.GetDirectory("songs"), false);
          var entitlementNames = new List<string>();
          foreach (DLCSong song in songs)
          {
            var shortname = prefix + song.SongData.Shortname;
            Directory.CreateDirectory(tempDir);
            var filePrefix = $"pc/songs_download/{shortname}/{shortname}";
            var arks = new List<List<Tuple<string, IFile, int>>>();
            arks.Add(new List<Tuple<string, IFile, int>>());

            // convert mogg.dta to dta_pc
            using (FileStream fs = File.OpenWrite(Path.Combine(tempDir, $"{shortname}.mogg.dta_dta_pc")))
              DTX.ToDtb(song.MoggDta, fs, 3, false);
            arks[0].Add(Tuple.Create($"{filePrefix}.mogg.dta_dta_pc", Util.LocalFile($"{tempDir}/{shortname}.mogg.dta_dta_pc"), -1));
            // convert moggsong to dta_pc
            using (FileStream fs = File.OpenWrite(Path.Combine(tempDir, $"{shortname}.moggsong_dta_pc")))
              DTX.ToDtb(song.MoggSong, fs, 3, false);
            arks[0].Add(Tuple.Create($"{filePrefix}.moggsong_dta_pc", Util.LocalFile($"{tempDir}/{shortname}.moggsong_dta_pc"), -1));
            // add empty vrevents to rbmid
            song.RBMidi.Format = RBMid.FORMAT_RBVR;
            song.RBMidi.VREvents = new RBMid.RBVREVENTS();
            using (var rbmid = File.OpenWrite(Path.Combine(tempDir, $"{shortname}.rbmid_pc")))
              RBMidWriter.WriteStream(song.RBMidi, rbmid);
            arks[0].Add(Tuple.Create($"{filePrefix}.rbmid_pc", Util.LocalFile($"{tempDir}/{shortname}.rbmid_pc"), -1));
            // add rbsong
            using (var rbsongFile = File.OpenWrite(Path.Combine(tempDir, $"{shortname}.rbsong")))
              new RBSongResourceWriter(rbsongFile).WriteStream(song.RBSong);
            arks[0].Add(Tuple.Create($"{filePrefix}.rbsong", Util.LocalFile($"{tempDir}/{shortname}.rbsong"), -1));
            // add songdta
            song.SongData.KeysRank = 0;
            song.SongData.RealKeysRank = 0;
            using (var songdtaFile = File.OpenWrite(Path.Combine(tempDir, $"{shortname}.songdta_pc")))
              SongDataWriter.WriteStream(song.SongData, songdtaFile);
            arks[0].Add(Tuple.Create($"{filePrefix}.songdta_pc", Util.LocalFile($"{tempDir}/{shortname}.songdta_pc"), -1));
            // add album art (incompatible)
            if (song.SongData.AlbumArt)
            {
              using (var artFile = File.OpenWrite(Path.Combine(tempDir, $"{shortname}.png_pc")))
                TextureWriter.WriteStream(song.Artwork, artFile);
              arks[0].Add(Tuple.Create($"{filePrefix}.png_pc", Util.LocalFile($"{tempDir}/{shortname}.png_pc"), -1));
            }
            // add mogg
            arks[0].Add(Tuple.Create($"{filePrefix}.mogg", song.Mogg, -1));

            var builder = new ArkBuilder($"{shortname}_pc", arks);
            log($"Writing {shortname}_pc.hdr and ARK files to {rbvrPath}...");
            builder.Save(rbvrPath, 0x0);
            entitlementNames.Add($"DLC = song_{shortname}");
            Directory.Delete(tempDir, true);
            conFile.Dispose();
          }
            log("Conversion complete! Add the following DLC SKUs to your entitlement list:");
            foreach (string sku in entitlementNames)
            {
              log(sku);
              File.WriteAllLines(rbvrPath + "/SKUs.txt", entitlementNames);
            }
            log($"You can also find the SKUs in {rbvrPath}\\SKUs.txt");
          
        }
      }
      else
      {
        log("converting CONs to rawfiles...");
        var cons = listBox1.Items.OfType<string>().Select(f => STFSPackage.OpenFile(GameArchives.Util.LocalFile(f))).ToList();
        foreach (var conFile in cons)
        {
          var songs = PkgCreator.ConvertDLCPackage(conFile.RootDirectory.GetDirectory("songs"), false);
          foreach (DLCSong song in songs)
          {
            var shortname = prefix + song.SongData.Shortname;
            log($"Writing songs\\pc\\songs\\{shortname} folder to {rbvrPath}...");

            var songPath = Path.Combine(rbvrPath, "songs", "pc", "songs", shortname);
            Directory.CreateDirectory(songPath);
            // add mogg
            using (var mogg = File.OpenWrite(Path.Combine(songPath, $"{shortname}.mogg")))
            using (var conMogg = song.Mogg.GetStream())
            {
              conMogg.CopyTo(mogg);
            }
            // convert mogg.dta to dta_pc
            using (FileStream fs = File.OpenWrite(Path.Combine(songPath, $"{shortname}.mogg.dta_dta_pc")))
              DTX.ToDtb(song.MoggDta, fs, 3, false);
            // convert moggsong to dta_pc
            using (FileStream fs = File.OpenWrite(Path.Combine(songPath, $"{shortname}.moggsong_dta_pc")))
              DTX.ToDtb(song.MoggSong, fs, 3, false);
            // add empty vrevents to rbmid
            song.RBMidi.Format = RBMid.FORMAT_RBVR;
            song.RBMidi.VREvents = new RBMid.RBVREVENTS();
            using (var rbmid = File.OpenWrite(Path.Combine(songPath, $"{shortname}.rbmid_pc")))
              RBMidWriter.WriteStream(song.RBMidi, rbmid);
            // add rbsong
            using (var rbsongFile = File.OpenWrite(Path.Combine(songPath, $"{shortname}.rbsong")))
              new RBSongResourceWriter(rbsongFile).WriteStream(song.RBSong);
            // add songdta
            song.SongData.KeysRank = 0;
            song.SongData.RealKeysRank = 0;
            using (var songdtaFile = File.OpenWrite(Path.Combine(songPath, $"{shortname}.songdta_pc")))
              SongDataWriter.WriteStream(song.SongData, songdtaFile);
            // add album art
            if (song.SongData.AlbumArt)
            {
              using (var artFile = File.OpenWrite(Path.Combine(songPath, $"{shortname}.png_pc")))
                TextureWriter.WriteStream(song.Artwork, artFile);
            }
            conFile.Dispose();
          }
          log("Conversion complete! Add the songs folder to your main RBVR ark using patchcreator in arkhelper");
        }
      }
    }

    private void listBox1_DragEnter(object sender, DragEventArgs e)
    {
      e.Effect = DragDropEffects.Copy;
    }

    private void listBox1_DragDrop(object sender, DragEventArgs e)
    {
      if (e.Data.GetData(DataFormats.FileDrop) is string[] files)
      {
        LoadCons(files);
      }
    }

    private void clearButton_Click(object sender, EventArgs e)
    {
      ClearState();
    }

    private void listBox1_KeyUp(object sender, KeyEventArgs e)
    {
      if (e.KeyCode == Keys.Delete || e.KeyCode == Keys.Back)
      {
        RemoveCon(listBox1.SelectedItem as string);
        UpdateState();
      }
    }

    private void Outputbtn_Click(object sender, EventArgs e)
    {
      using (var sfd = new FolderBrowserDialog())
      {
        if (sfd.ShowDialog() == DialogResult.OK)
        {
          folderName.Text = sfd.SelectedPath;
          groupBox3.Enabled = true;
        }
      }
    }
  }
}
