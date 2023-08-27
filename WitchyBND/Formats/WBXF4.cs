using System;
using System.IO;
using System.Xml;
using SoulsFormats;

namespace WitchyBND;

internal static class WBXF4
{
    public static void Unpack(this BXF4Reader bxf, string bhdName, string bdtName, string targetDir,
        IProgress<float> progress)
    {
        Directory.CreateDirectory(targetDir);
        var xws = new XmlWriterSettings();
        xws.Indent = true;
        var xw = XmlWriter.Create($"{targetDir}\\_witchy-bxf4.xml", xws);
        xw.WriteStartElement("bxf4");

        xw.WriteElementString("bhd_filename", bhdName);
        xw.WriteElementString("bdt_filename", bdtName);
        xw.WriteElementString("version", bxf.Version);
        xw.WriteElementString("format", bxf.Format.ToString());
        xw.WriteElementString("bigendian", bxf.BigEndian.ToString());
        xw.WriteElementString("bitbigendian", bxf.BitBigEndian.ToString());
        xw.WriteElementString("unicode", bxf.Unicode.ToString());
        xw.WriteElementString("extended", $"0x{bxf.Extended:X2}");
        xw.WriteElementString("unk04", bxf.Unk04.ToString());
        xw.WriteElementString("unk05", bxf.Unk05.ToString());

        WBinder.WriteBinderFiles(bxf, xw, targetDir, progress);
        xw.WriteEndElement();
        xw.Close();
    }

    public static void Repack(string sourceDir, string targetDir)
    {
        var bxf = new BXF4();
        var xml = new XmlDocument();

        xml.Load(WBUtil.GetXmlPath("bxf4", sourceDir));

        var bhdFilename = xml.SelectSingleNode("bxf4/bhd_filename").InnerText;
        var bdtFilename = xml.SelectSingleNode("bxf4/bdt_filename").InnerText;
        var root = xml.SelectSingleNode("bxf4/root")?.InnerText ?? "";

        bxf.Version = xml.SelectSingleNode("bxf4/version").InnerText;
        bxf.Format = (Binder.Format)Enum.Parse(typeof(Binder.Format), xml.SelectSingleNode("bxf4/format").InnerText);
        bxf.BigEndian = bool.Parse(xml.SelectSingleNode("bxf4/bigendian").InnerText);
        bxf.BitBigEndian = bool.Parse(xml.SelectSingleNode("bxf4/bitbigendian").InnerText);
        bxf.Unicode = bool.Parse(xml.SelectSingleNode("bxf4/unicode").InnerText);
        bxf.Extended = Convert.ToByte(xml.SelectSingleNode("bxf4/extended").InnerText, 16);
        bxf.Unk04 = bool.Parse(xml.SelectSingleNode("bxf4/unk04").InnerText);
        bxf.Unk05 = bool.Parse(xml.SelectSingleNode("bxf4/unk05").InnerText);

        WBinder.ReadBinderFiles(bxf, xml.SelectSingleNode("bxf4/files"), sourceDir, root);

        var bhdPath = $"{targetDir}\\{bhdFilename}";
        WBUtil.Backup(bhdPath);
        var bdtPath = $"{targetDir}\\{bdtFilename}";
        WBUtil.Backup(bdtPath);
        bxf.Write(bhdPath, bdtPath);
    }
}