using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using DtxCS;
using DtxCS.DataTypes;
using GameArchives;
using GameArchives.STFS;
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

namespace ForgeTool
{
  class Program
  {
    static void Main(string[] args)
    {
      if (args.Length < 1)
      {
        Usage();
        return;
      }
      void WithIO(Action<Stream, Stream> action)
      {
        using (var fi = File.OpenRead(args[1]))
        using (var fo = File.OpenWrite(args[2]))
          action(fi, fo);
      }
      var makeark = true;
      var makepkg = true;
      switch (args[0])
      {
        case "rbmid2mid":
          WithIO((fi, fo) =>
          {
            var rbmid = RBMidReader.ReadStream(fi);
            var midi = RBMidConverter.ToMid(rbmid);
            MidiCS.MidiFileWriter.WriteSMF(midi, fo);
          });
          break;
        case "mid2rbmid":
          WithIO((fi, fo) =>
          {
            var mid = MidiCS.MidiFileReader.FromStream(fi);
            var rbmid = RBMidConverter.ToRBMid(mid);
            RBMidWriter.WriteStream(rbmid, fo);
          });
          break;
        case "reprocess":
          WithIO((fi, fo) =>
          {
            var rbmid = RBMidReader.ReadStream(fi);
            var processed = RBMidConverter.ToRBMid(RBMidConverter.ToMid(rbmid), rbmid.HopoThreshold);
            RBMidWriter.WriteStream(processed, fo);
          });
          break;
        case "tex2png":
          WithIO((fi, fo) =>
          {
            var compressionType = "default";
            if(args.Length > 3)  
            {
              compressionType = args[3];
            }
            var tex = TextureReader.ReadStream(fi);
            var bitmap = TextureConverter.ToBitmap(tex, 0, compressionType);
            bitmap.Save(fo, System.Drawing.Imaging.ImageFormat.Png);
          });
          break;
        case "png2tex":
          WithIO((fi, fo) =>
          {
            var img = System.Drawing.Image.FromStream(fi);
            var tex = TextureConverter.ToTexture(img);
            TextureWriter.WriteStream(tex, fo);
          });
          break;
        case "milopng2tex":
          WithIO((fi, fo) =>
          {
            var tex = TextureConverter.MiloPngToTexture(fi);
            TextureWriter.WriteStream(tex, fo);
          });
          break;
        case "mesh2obj":
          {
            var input = args[1];
            var output = args[2];
            using (var fi = File.OpenRead(input))
            {
              var mesh = HxMeshReader.ReadStream(fi);
              var obj = HxMeshConverter.ToObj(mesh);
              File.WriteAllText(output, obj);
            }
            break;
          }
        case "milo2lip":
        case "milo2lips":
        case "milo2lipsync":
          WithIO((fi, fo) =>
          {
            var milo = MiloFile.ReadFromStream(fi);
            var lipsync = LipsyncConverter.FromMilo(milo);
            new LipsyncWriter(fo).WriteStream(lipsync);
          });
          break;
        case "version":
          var assembly = System.Reflection.Assembly.GetExecutingAssembly();
          var version = FileVersionInfo.GetVersionInfo(assembly.Location).FileVersion;
          var libAssembly = System.Reflection.Assembly.GetAssembly(typeof(RBMid));
          var libVersion = FileVersionInfo.GetVersionInfo(libAssembly.Location).FileVersion;
          Console.WriteLine($"ForgeTool v{version}");
          Console.WriteLine($"LibForge v{libVersion}");
          break;
        case "test":
          {
            var dir = args[1];
            int succ = 0, warn = 0, fail = 0;
            var files = dir.EndsWith(".rbmid_ps4") || dir.EndsWith(".rbmid_pc") ?
              new[] { dir } : Directory.EnumerateFiles(dir, "*.rbmid_*");
            foreach (var f in files)
            {
              var info = new FileInfo(f);
              var name = info.Name;
              using (var fi = File.OpenRead(f))
              {
                try
                {
                  var rbmid = RBMidReader.ReadStream(fi);
                  var midi = RBMidConverter.ToMid(rbmid);
                  var rbmid2 = RBMidConverter.ToRBMid(midi, rbmid.HopoThreshold);
                  using (var ms = new MemoryStream((int)fi.Length))
                  {
                    // TODO: Switch this to rbmid2 once we are generating all events
                    //       (regardless of whether the event data are all correct)
                    RBMidWriter.WriteStream(rbmid, ms);
                    ms.Position = 0;
                    if (ms.Length == fi.Length)
                    {
                      var comparison = rbmid.Compare(rbmid2);
                      if (comparison != null)
                      {
                        throw new CompareException("File comparison failed at field: " + comparison);
                      }
                      Console.WriteLine($"[OK] {name}");
                      succ++;
                    }
                    else
                    {
                      Console.Write($"[WARN] {name}:");
                      Console.WriteLine($" Processed file had different length ({fi.Length} orig, {ms.Length} processed)");
                      warn++;
                    }
                  }
                } catch (CompareException e)
                {
                  Console.WriteLine($"[WARN] {name}: {e.Message}");
                  warn++;
                }
                catch (Exception e)
                {
                  Console.WriteLine($"[ERROR] {name}: {e.Message}");
                  Console.WriteLine(e.StackTrace);
                  fail++;
                }
              }
            }
            Console.WriteLine($"Summary: {succ} OK, {warn} WARN, {fail} ERROR");
            if (warn > 0)
            {
              Console.WriteLine("(a = converted file, b = original)");
            }
            if (warn > 0 || fail > 0)
            {
              Environment.Exit(1);
            }
          }
          break;
        case "simpletest":
          {
            var dir = args[1];
            int succ = 0, warn = 0, fail = 0;
            foreach (var f in Directory.EnumerateFiles(dir, "*.rbmid_*"))
            {
              var info = new FileInfo(f);
              var name = info.Name;
              using (var fi = File.OpenRead(f))
              {
                try
                {
                  var rbmid = RBMidReader.ReadStream(fi);
                  using (var ms = new MemoryStream((int)fi.Length))
                  {
                    RBMidWriter.WriteStream(rbmid, ms);
                    ms.Position = 0;
                    if (ms.Length == fi.Length)
                    {
                      succ++;
                    }
                    else
                    {
                      Console.WriteLine($"[WARN] {name}:");
                      Console.WriteLine($"  Processed file had different length ({fi.Length} orig, {ms.Length} processed)");
                      warn++;
                    }
                  }
                }
                catch (Exception e)
                {
                  Console.WriteLine($"[ERROR] {name}:");
                  Console.WriteLine("  " + e.Message);
                  fail++;
                }
              }
            }
            Console.WriteLine($"Summary: {succ} OK, {warn} WARN, {fail} ERROR");
          }
          break;
        case "testrbsong":
          {
            var dir = args[1];
            int succ = 0, warn = 0, fail = 0;
            var files = dir.EndsWith(".rbsong") ?
              new[] { dir } : Directory.EnumerateFiles(dir, "*.rbsong");
            foreach (var f in files)
            {
              var info = new FileInfo(f);
              var name = info.Name;
              using (var fi = File.OpenRead(f))
              {
                try
                {
                  var rbsong = new RBSongResource();
                  rbsong.Load(fi);
                  using (var ms = new MemoryStream((int)fi.Length))
                  {
                    new RBSongResourceWriter(ms).WriteStream(rbsong);
                    ms.Position = 0;
                    if (ms.Length == fi.Length)
                    {
                      succ++;
                    }
                    else
                    {
                      Console.WriteLine($"[WARN] {name}:");
                      Console.WriteLine($"  Processed file had different length ({fi.Length} orig, {ms.Length} processed)");
                      warn++;
                    }
                  }
                }
                catch (Exception e)
                {
                  Console.WriteLine($"[ERROR] {name}:");
                  Console.WriteLine("  " + e.Message);
                  fail++;
                }
              }
            }
            Console.WriteLine($"Summary: {succ} OK, {warn} WARN, {fail} ERROR");
            if (fail > 0)
            {
              Console.WriteLine("(a = converted file, b = original)");
            }
          }
          break;
        case "rbsong2rbsong":
          WithIO((i, o) => {
            var rbsong = new RBSongResource();
            rbsong.Load(i);
            new RBSongResourceWriter(o).WriteStream(rbsong);
          });
          break;
        case "con2gp4":
          makepkg = false;
          goto case "con2pkg";
        case "con2pkg":
          {
            var i = 0;
            bool eu = false;
            string pkgId = null, pkgDesc = null;
            while (++i < args.Length - 3)
            {
              switch (args[i])
              {
                case "--scee":
                  eu = true;
                  continue;
                case "--id":
                  pkgId = args[++i];
                  continue;
                case "--desc":
                  pkgDesc = args[++i];
                  continue;
              }
              break;
            }
            if(makepkg)
            {
              var con = args[i++];
              var dest = args[i++];
              var tmpDir = Path.Combine(Path.GetTempPath(), "forgetool_tmp_build");
              if (!Directory.Exists(con))
              {
                var conFilename = Path.GetFileName(con);
                PkgCreator.ConToGp4(con, tmpDir, eu, pkgId, pkgDesc);
              }
              else
              {
                PkgCreator.ConsToGp4(con, tmpDir, eu, pkgId, pkgDesc); 
              }
              PkgCreator.BuildPkg(Path.Combine(tmpDir, "project.gp4"), dest);
              Directory.Delete(tmpDir, true);
            }
            else
            {
              var con = args[i++];
              var dest = args[i++];
              PkgCreator.ConToGp4(con, dest, eu, pkgId, pkgDesc);
            }
          }
          break;
        case "con2rbvr":
        case "con2vr":
          {
            var i = 0;
            string prefix = null;
            while (++i < args.Length - 2)
            {
              switch (args[i])
              {
                case "--noark":
                  makeark = false;
                  continue;
                case "--prefix":
                  prefix = args[++i];
                  continue;
              }
              break;
            }
            var conPath = args[i++];
            var rbvrPath = args[i++];
            var conFile = STFSPackage.OpenFile(Util.LocalFile(conPath));
            var tempDir = Path.Combine(Path.GetTempPath(), "forgetool_tmp_build");
            var songs = PkgCreator.ConvertDLCPackage(conFile.RootDirectory.GetDirectory("songs"), false);
            var entitlementNames = new List<string>();

            foreach (DLCSong song in songs)
            {
              var shortname = prefix + song.SongData.Shortname;

              if (makeark)
              {
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
                Console.WriteLine($"Writing {shortname}_pc.hdr and ARK files to {rbvrPath}...");
                builder.Save(rbvrPath, 0x0);
                entitlementNames.Add($"DLC = song_{shortname}");
                Directory.Delete(tempDir, true);
              }
              else
              {
                Console.WriteLine($"Writing songs\\pc\\songs\\{shortname} folder to {rbvrPath}...");

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
              }
            }
            if (makeark)
            {
              Console.WriteLine("Conversion complete! Add the following DLC SKUs to your entitlement list:");
              foreach (string sku in entitlementNames)
              {
                Console.WriteLine(sku);
                File.WriteAllLines(rbvrPath + "/SKUs.txt", entitlementNames);
              }
              Console.WriteLine($"You can also find the SKUs in {rbvrPath}\\SKUs.txt");
            }
            else
            {
              Console.WriteLine("Conversion complete! Add the songs folder to your main RBVR ark using patchcreator in arkhelper");
            }
          }
          break;
        case "dta2b":
          WithIO((fi, fo) =>
          {
            DataArray dta = DTX.FromDtaStream(fi);
            DTX.ToDtb(dta, fo, 3, false);
          });
          break;
        case "csv2txt":
          {
            var input = args[1];
            var output = args[2];
            using (var fi = File.OpenRead(input))
            {
              var csv = CsvData.LoadFile(fi);
              File.WriteAllText(output, csv.ToString());
            }
          }
          break;
        case "arkorder":
          {
            var input = args[1];
            var output = args[2];
            var sb = new StringBuilder();
            var archive = new Archive(input);
            using (var o = File.OpenWrite(output))
            using (var sw = new StreamWriter(o))
            {
              archive.WriteArkorder(sw);
            }
          }
          break;
        case "arkbuild":
          {
            var filedir = args[1];
            var arkorder = args[2];
            var outdir = args[3];
            var name = args[4];
            if (!Directory.Exists(filedir))
            {
              Console.WriteLine($"Error: {filedir} does not exist.");
              return;
            }
            if (!Directory.Exists(outdir))
            {
              Console.WriteLine($"Error: {outdir} does not exist.");
              return;
            }
            if (!File.Exists(arkorder))
            {
              Console.WriteLine($"Error: {arkorder} not found.");
              return;
            }
            var arks = new List<List<Tuple<string, IFile, int>>>();
            DataArray arkOrder;
            using (var order = File.OpenRead(arkorder))
            using (var reader = new StreamReader(order))
            {
              arkOrder = DtxCS.DTX.FromDtaStream(order);
            }
            int arkIndex = 0;
            arks.Add(new List<Tuple<string, IFile, int>>());
            foreach(var x in arkOrder.Children)
            {
              if (x is DataCommand c && c.Symbol(0).Name == "split_ark")
              {
                arkIndex++;
                arks.Add(new List<Tuple<string, IFile, int>>());
              } else if (x is DataArray s)
              {
                var fullPath = Path.Combine(filedir, s.Name.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(fullPath))
                {
                  Console.WriteLine($"Error: {fullPath} could not be found.");
                  return;
                }
                IFile f = Util.LocalFile(fullPath);
                arks[arkIndex].Add(Tuple.Create(s.Name, f, s.Int(1)));
              }
            }
            var builder = new ArkBuilder(name, arks);
            Console.Write($"Writing {name}.hdr and ark files to {outdir}...");
            builder.Save(outdir);
          }
          break;
        default:
          Usage();
          break;
      }
    }

    static void Usage()
    {
      Console.WriteLine("Usage: ForgeTool.exe <verb> [options]");
      Console.WriteLine("Verbs: ");
      Console.WriteLine("  version");
      Console.WriteLine("    - Prints the version number and exits");
      Console.WriteLine("  rbmid2mid <input.rbmid> <output.mid>");
      Console.WriteLine("   - converts a Forge midi to a Standard Midi File");
      Console.WriteLine("  reprocess <input.rbmid> <output.rbmid>");
      Console.WriteLine("   - converts a Forge midi to a Forge midi");
      Console.WriteLine("  mid2rbmid <input.mid> <output.rbmid>");
      Console.WriteLine("   - converts a Standard Midi File to a Forge midi");
      Console.WriteLine("  tex2png <input.png/bmp_pc/ps4> <output.png>");
      Console.WriteLine("   - converts a Forge texture to PNG");
      Console.WriteLine("  mesh2obj <input.fbx...> <output.obj>");
      Console.WriteLine("   - converts a Forge mesh to OBJ");
      Console.WriteLine("  con2gp4 [--scee] [--id 16CHARIDENTIFIER] [--desc \"Package Description\"] <input_con> <output_dir>");
      Console.WriteLine("   - converts a CON custom to a .gp4 project in the given output directory");
      Console.WriteLine("       --scee : make an EU package");
      Console.WriteLine("       --id <16CHARIDENTIFIER> : set the customizable part of the Package ID/Filename");
      Console.WriteLine("       --desc \"Package Description\" : set the description of the package");
      Console.WriteLine("  con2pkg [--scee] [--id 16CHARIDENTIFIER] [--desc \"Package Description\"] <input_con> <output_dir>");
      Console.WriteLine("   - converts a CON custom to a PS4 PKG custom in the given output directory");
      Console.WriteLine("       --scee : make an EU package");
      Console.WriteLine("       --id <16CHARIDENTIFIER> : set the customizable part of the Package ID/Filename");
      Console.WriteLine("       --desc \"Package Description\" : set the description of the package");
      Console.WriteLine("  con2rbvr [--noark] [--prefix PREFIX] <input_con> <output_dir>");
      Console.WriteLine("   - converts a CON custom to a RBVR custom in the given game directory");
      Console.WriteLine("       --noark : creates a songs folder to be used with patchcreator in arkhelper instead of a DLC ARK");
      Console.WriteLine("       --prefix <PREFIX> : sets a custom prefix to better organize your converted song files");
      Console.WriteLine("  arkorder <input_hdr> <output_dta>");
      Console.WriteLine("   - outputs the arkorder DTA of a given ARK HDR file");
      Console.WriteLine("  arkbuild <input_dir> <input_arkorder> <output_dir> <ark_name>");
      Console.WriteLine("   - creates an ARK file using files with the specified arkorder in the output directory");
      Console.WriteLine("  milo2lipsync <input.milo_xbox> <output.lipsync>");
      Console.WriteLine("   - converts an uncompressed milo archive to forge lipsync file");
      Console.WriteLine("  csv2txt <input.csv_pc> <output.csv>");
      Console.WriteLine("   - decodes a csv file");
      Console.WriteLine("  dta2b <input.dta> <output.dta_dta_pc/ps4>");
      Console.WriteLine("   - converts text dta into encoded dta");
    }
  }
  class CompareException : Exception {
    public CompareException(string msg) : base(msg) { }
  }
}
