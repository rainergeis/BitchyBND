using System.Xml;
using SoulsFormats;

namespace WitchyBND;

internal static class WLUAGNL
{
    public static void Unpack(this LUAGNL gnl, string sourceFile)
    {
        var xws = new XmlWriterSettings();
        xws.Indent = true;
        var xw = XmlWriter.Create($"{sourceFile}.xml", xws);
        xw.WriteStartElement("luagnl");
        xw.WriteElementString("bigendian", gnl.BigEndian.ToString());
        xw.WriteElementString("longformat", gnl.LongFormat.ToString());
        xw.WriteStartElement("globals");

        foreach (var global in gnl.Globals) xw.WriteElementString("global", global);

        xw.WriteEndElement();
        xw.WriteEndElement();
        xw.Close();
    }

    public static void Repack(string sourceFile)
    {
        var gnl = new LUAGNL();
        var xml = new XmlDocument();
        xml.Load(sourceFile);
        gnl.BigEndian = bool.Parse(xml.SelectSingleNode("luagnl/bigendian").InnerText);
        gnl.LongFormat = bool.Parse(xml.SelectSingleNode("luagnl/longformat").InnerText);

        foreach (XmlNode node in xml.SelectNodes("luagnl/globals/global")) gnl.Globals.Add(node.InnerText);

        var outPath = sourceFile.Replace(".luagnl.xml", ".luagnl");
        WBUtil.Backup(outPath);
        gnl.Write(outPath);
    }
}