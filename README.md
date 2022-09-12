# About This Repository

This is a revival of LibForge, in order to renew the code and also make it compatible with Amplitude 2016.

# How to Build

You need: .NET 6 | .NET Core 3.1

Compile using LibForge.sln, when the compilation is complete, there should be a bin folder inside each program folder. (ForgeTool, ForgeToolGUI, LibForge, LibForgeTests)

## ForgeTool

This is a command line tool that does file conversions.

```
Usage: ForgeTool.exe <verb> [options]
Verbs:
  version
    - Prints the version number and exits
  rbmid2mid <input.rbmid> <output.mid>
   - converts a Forge midi to a Standard Midi File
  reprocess <input.rbmid> <output.rbmid>
   - converts a Forge midi to a Forge midi
  mid2rbmid <input.mid> <output.rbmid>
   - converts a Standard Midi File to a Forge midi
  tex2png <input.png/bmp_pc/ps4> <output.png>
   - converts a Forge texture to PNG
  mesh2obj <input.fbx...> <output.obj>
   - converts a Forge mesh to OBJ
  con2gp4 [--scee] [--id 16CHARIDENTIFIER] [--desc "Package Description"] <input_con> <output_dir>
   - converts a CON custom to a .gp4 project in the given output directory
       --scee : make an EU package
       --id <16CHARIDENTIFIER> : set the customizable part of the Package ID/Filename
       --desc "Package Description" : set the description of the package
  con2pkg [--scee] [--id 16CHARIDENTIFIER] [--desc "Package Description"] <input_con> <output_dir>
   - converts a CON custom to a PS4 PKG custom in the given output directory
       --scee : make an EU package
       --id <16CHARIDENTIFIER> : set the customizable part of the Package ID/Filename
       --desc "Package Description" : set the description of the package
  milo2lipsync <input.milo_xbox> <output.lipsync>
   - converts an uncompressed milo archive to forge lipsync file
```

## ForgeToolGUI

This has an ark/PFS/folder browser with support for previewing the following:
  - Textures (.png_pc, .png_ps4, .bmp_pc, .bmp_ps4, etc)
  - Models (a 3d preview with optional wireframe)
  - .songdta files
  - Data (.dta/.dtb/.\*\_dta\_\*/.moggsong) files
  - RBmid files
  - RBsong files
