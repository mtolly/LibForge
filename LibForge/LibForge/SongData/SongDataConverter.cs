using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DtxCS.DataTypes;

namespace LibForge.SongData
{
  public enum Decade
  {
    the30s = 1930,
    the40s = 1940,
    the50s = 1950,
    the60s = 1960,
    the70s = 1970,
    the80s = 1980,
    the90s = 1990,
    the00s = 2000,
    the10s = 2010,
    the20s = 2020
  }
  public class SongDataConverter
  {

    public static SongData ToSongData(DataArray songDta, GameArchives.IDirectory dlcRoot, Boolean isSongRoot = false)
    {
      var songId = songDta.Array("song_id");
      var art = songDta.Array("album_art")?.Any(1) ?? "FALSE";
      var songName = songDta.Array("song").Array("name")?.String(1) ?? songDta.Array("song").Array("name")?.Symbol(1).ToString();
      var shortName = songName.Split('/').Last();
      var songIdNum = (shortName.GetHashCode() & 0xFFFFFF) + 90000000;
      // Get MIDI duration dta song_length is not defined, round just to make sure...
      string subPath = "";
      if(!isSongRoot)
      {
        subPath = shortName + "/";
      }
      var midi = MidiCS.MidiFileReader.FromBytes(dlcRoot.GetFileAtPath(subPath + shortName + ".mid").GetBytes());
      var midiDuration = (int)(Math.Ceiling(midi.Duration)* 1000);


      var songLength = songDta.Array("song_length")?.Int(1) ?? midiDuration;

      
      string decade = songDta.Array("decade")?.Any(1).ToString() ?? "the00s";
      var decadeInt = (int)Enum.Parse(typeof(Decade), decade);


      var albumArt = art == "1" || art == "TRUE";
      var albumName = songDta.Array("album_name")?.String(1) ?? "";
      var albumTrackNumber = (short)(songDta.Array("album_track_number")?.Int(1) ?? 0);
      var albumYear = songDta.Array("year_released")?.Int(1) ?? 0;
      var artist = songDta.Array("artist").String(1);
      var bandRank = songDta.Array("rank").Array("band").Int(1);
      var bassRank = songDta.Array("rank").Array("bass").Int(1);
      var drumRank = songDta.Array("rank").Array("drum").Int(1);
      var guitarRank = songDta.Array("rank").Array("guitar").Int(1);
      var keysRank = songDta.Array("rank").Array("keys")?.Int(1) ?? 0;
      var realKeysRank = songDta.Array("rank").Array("real_keys")?.Int(1) ?? 0;
      var vocalsRank = songDta.Array("rank").Array("vocals").Int(1);
      var gameOrigin = songDta.Array("game_origin")?.Any(1) ?? "ugc_plus";
      var genre = songDta.Array("genre").Symbol(1).ToString();
      var name = songDta.Array("name").String(1);
      var originalYear = songDta.Array("year_released")?.Int(1) ?? decadeInt;
      var vocalGender = (byte)((songDta.Array("vocal_gender")?.Any(1) ?? "male") == "male" ? 1 : 2);
      var vocalParts = songDta.Array("song").Array("vocal_parts")?.Int(1) ?? 1;
      var shortname = shortName.ToLowerInvariant();
      var previewStart = songDta.Array("preview").Int(1);
      var previewEnd = songDta.Array("preview").Int(2);


      return new SongData
      {
        AlbumArt = albumArt,
        AlbumName = albumName,
        AlbumTrackNumber = albumTrackNumber,
        AlbumYear = albumYear,
        Artist = artist,
        BandRank = bandRank,
        BassRank = bassRank,
        DrumRank = drumRank,
        GuitarRank = guitarRank,
        KeysRank = keysRank,
        RealKeysRank = realKeysRank,
        VocalsRank = vocalsRank,
        Cover = false,
        Fake = false,
        Flags = 0,
        GameOrigin = gameOrigin,
        Genre = genre,
        HasFreestyleVocals = false,
        Medium = "",
        Name = name,
        OriginalYear = originalYear,
        Tutorial = false,
        Type = 11,
        Version = -1,
        VocalGender = vocalGender,
        VocalParts = vocalParts,
        Shortname = shortname,
        SongId = (uint)songIdNum,
        SongLength = songLength,
        PreviewStart = previewStart,
        PreviewEnd = previewEnd,
      };
    }
  }
}
