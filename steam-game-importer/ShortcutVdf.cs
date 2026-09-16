using System.Text;

namespace SteamGameImporter;

internal sealed class SteamShortcut
{
    public string AppName { get; set; } = "";
    public string Exe { get; set; } = "";
    public string StartDir { get; set; } = "";
    public string LaunchOptions { get; set; } = "";
}

internal static class ShortcutVdf
{
    private const byte Object = 0x00, String = 0x01, Int32 = 0x02, End = 0x08;

    public static List<SteamShortcut> Read(string path)
    {
        if (!File.Exists(path)) return [];
        using var stream = File.OpenRead(path);
        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: false);
        var result = new List<SteamShortcut>();
        try
        {
            if (reader.ReadByte() != Object || ReadCString(reader) != "shortcuts") return result;
            while (stream.Position < stream.Length)
            {
                byte type = reader.ReadByte();
                if (type == End) break;
                if (type != Object) { SkipValue(reader, type); continue; }
                _ = ReadCString(reader);
                var item = ReadShortcut(reader);
                if (!string.IsNullOrWhiteSpace(item.Exe)) result.Add(item);
            }
        }
        catch (EndOfStreamException) { }
        return result;
    }

    private static SteamShortcut ReadShortcut(BinaryReader reader)
    {
        var item = new SteamShortcut();
        while (true)
        {
            byte type = reader.ReadByte();
            if (type == End) break;
            string key = ReadCString(reader);
            if (type == String)
            {
                string value = ReadCString(reader);
                switch (key.ToLowerInvariant())
                {
                    case "appname": item.AppName = value; break;
                    case "exe": item.Exe = value; break;
                    case "startdir": item.StartDir = value; break;
                    case "launchoptions": item.LaunchOptions = value; break;
                }
            }
            else SkipValue(reader, type, keyAlreadyRead: true);
        }
        return item;
    }

    public static void Write(string path, IReadOnlyList<SteamShortcut> shortcuts)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string temporary = path + ".tmp";
        using (var writer = new BinaryWriter(File.Create(temporary), Encoding.UTF8, leaveOpen: false))
        {
            WriteObject(writer, "shortcuts");
            for (int i = 0; i < shortcuts.Count; i++)
            {
                WriteObject(writer, i.ToString());
                WriteInt(writer, "appid", ComputeAppId(shortcuts[i].Exe, shortcuts[i].AppName));
                // A Steam kliens ezeket a kulcsokat pontosan kisbetűsen írja és várja.
                WriteString(writer, "appname", shortcuts[i].AppName);
                WriteString(writer, "exe", shortcuts[i].Exe);
                WriteString(writer, "StartDir", shortcuts[i].StartDir);
                WriteString(writer, "icon", "");
                WriteString(writer, "ShortcutPath", "");
                WriteString(writer, "LaunchOptions", shortcuts[i].LaunchOptions);
                WriteInt(writer, "IsHidden", 0); WriteInt(writer, "AllowDesktopConfig", 1);
                WriteInt(writer, "AllowOverlay", 1); WriteInt(writer, "OpenVR", 0);
                WriteInt(writer, "Devkit", 0); WriteString(writer, "DevkitGameID", "");
                WriteInt(writer, "DevkitOverrideAppID", 0); WriteInt(writer, "LastPlayTime", 0);
                WriteString(writer, "FlatpakAppID", "");
                WriteObject(writer, "tags"); writer.Write(End);
                writer.Write(End);
            }
            writer.Write(End); // shortcuts objektum vége
            writer.Write(End); // bináris KeyValues dokumentum vége
        }
        File.Move(temporary, path, overwrite: true);
    }

    public static uint ComputeAppId(string exe, string name)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(exe + name);
        uint crc = 0xFFFFFFFF;
        foreach (byte b in bytes) { crc ^= b; for (int j = 0; j < 8; j++) crc = (crc >> 1) ^ (0xEDB88320u & (uint)-(int)(crc & 1)); }
        crc ^= 0xFFFFFFFF;
        return crc | 0x80000000u;
    }

    public static uint ComputeGridId(string exe, string name) => ComputeAppId(exe, name);
    private static void WriteObject(BinaryWriter w, string key) { w.Write(Object); WriteCString(w, key); }
    private static void WriteString(BinaryWriter w, string key, string value) { w.Write(String); WriteCString(w, key); WriteCString(w, value); }
    private static void WriteInt(BinaryWriter w, string key, uint value) { w.Write(Int32); WriteCString(w, key); w.Write(value); }
    private static void WriteCString(BinaryWriter w, string value) { w.Write(Encoding.UTF8.GetBytes(value)); w.Write((byte)0); }
    private static string ReadCString(BinaryReader r) { var b = new List<byte>(); byte x; while ((x = r.ReadByte()) != 0) b.Add(x); return Encoding.UTF8.GetString(b.ToArray()); }
    private static void SkipValue(BinaryReader r, byte type, bool keyAlreadyRead = false)
    {
        if (!keyAlreadyRead) _ = ReadCString(r);
        if (type == String) _ = ReadCString(r); else if (type == Int32) _ = r.ReadUInt32(); else if (type == Object) { int depth = 1; while (depth > 0) { byte t = r.ReadByte(); if (t == End) { depth--; continue; } _ = ReadCString(r); if (t == Object) depth++; else if (t == String) _ = ReadCString(r); else if (t == Int32) _ = r.ReadUInt32(); } }
    }
}
