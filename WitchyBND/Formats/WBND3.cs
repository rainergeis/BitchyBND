using System;
using System.IO;
using System.Xml;
using SoulsFormats;

namespace WitchyBND;

internal static class WBND3
{
    public static void Unpack(this BND3Reader bnd, string sourceName, string targetDir, IProgress<float> progress)
    {
        Directory.CreateDirectory(targetDir);
        var xws = new XmlWriterSettings();
        xws.Indent = true;
        var xw = XmlWriter.Create($"{targetDir}\\_witchy-bnd3.xml", xws);
        xw.WriteStartElement("bnd3");

        xw.WriteElementString("filename", sourceName);
        xw.WriteElementString("compression", bnd.Compression.ToString());
        xw.WriteElementString("version", bnd.Version);
        xw.WriteElementString("format", bnd.Format.ToString());
        xw.WriteElementString("bigendian", bnd.BigEndian.ToString());
        xw.WriteElementString("bitbigendian", bnd.BitBigEndian.ToString());
        xw.WriteElementString("unk18", $"0x{bnd.Unk18:X}");
        WBinder.WriteBinderFiles(bnd, xw, targetDir, progress);

        xw.WriteEndElement();
        xw.Close();
    }

    public static void Repack(string sourceDir, string targetDir)
    {
        var bnd = new BND3();
        var xml = new XmlDocument();

        xml.Load(WBUtil.GetXmlPath("bnd3", sourceDir));

        if (xml.SelectSingleNode("bnd3/filename") == null)
            throw new FriendlyException("Missing filename tag.");

        var filename = xml.SelectSingleNode("bnd3/filename").InnerText;
        var root = xml.SelectSingleNode("bnd3/root")?.InnerText ?? "";

        var strCompression = xml.SelectSingleNode("bnd3/compression")?.InnerText ?? "None";
        bnd.Version = xml.SelectSingleNode("bnd3/version")?.InnerText ?? "07D7R6";
        var strFormat = xml.SelectSingleNode("bnd3/format")?.InnerText ?? "IDs, Names1, Names2, Compression";
        var strBigEndian = xml.SelectSingleNode("bnd3/bigendian")?.InnerText ?? "False";
        var strBitBigEndian = xml.SelectSingleNode("bnd3/bitbigendian")?.InnerText ?? "False";
        var strUnk18 = xml.SelectSingleNode("bnd3/unk18")?.InnerText ?? "0x0";

        if (!Enum.TryParse(strCompression, out DCX.Type compression))
            throw new FriendlyException($"Could not parse compression type: {strCompression}");
        bnd.Compression = compression;

        try
        {
            bnd.Format = (Binder.Format)Enum.Parse(typeof(Binder.Format), strFormat);
        }
        catch
        {
            throw new FriendlyException(
                $"Could not parse format: {strFormat}\nFormat must be a comma-separated list of flags.");
        }

        if (!bool.TryParse(strBigEndian, out var bigEndian))
            throw new FriendlyException(
                $"Could not parse big-endianness: {strBigEndian}\nBig-endianness must be true or false.");
        bnd.BigEndian = bigEndian;

        if (!bool.TryParse(strBitBigEndian, out var bitBigEndian))
            throw new FriendlyException(
                $"Could not parse bit big-endianness: {strBitBigEndian}\nBit big-endianness must be true or false.");
        bnd.BitBigEndian = bitBigEndian;

        try
        {
            bnd.Unk18 = Convert.ToInt32(strUnk18, 16);
        }
        catch
        {
            throw new FriendlyException($"Could not parse unk18: {strUnk18}\nUnk18 must be a hex value.");
        }

        if (xml.SelectSingleNode("bnd3/files") != null)
            WBinder.ReadBinderFiles(bnd, xml.SelectSingleNode("bnd3/files"), sourceDir, root);

        var outPath = $"{targetDir}\\{filename}";
        WBUtil.Backup(outPath);
        bnd.Write(outPath);
    }
}