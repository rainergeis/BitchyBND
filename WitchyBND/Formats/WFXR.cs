using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using WitchyFormats;

namespace WitchyBND;

/**
 * Code mostly adapted from FXR3-XMLR.
 */
internal static class WFXR
{
    public static bool Unpack(this Fxr3 fxr, string sourceFile)
    {
        var XDoc = new XDocument();

        using (XmlWriter xmlWriter = XDoc.CreateWriter())
        {
            var thing = new XmlSerializer(typeof(Fxr3));
            thing.Serialize(xmlWriter, fxr);
        }

        XDoc.Save($"{sourceFile}.xml");

        return false;
    }

    public static bool Repack(string sourceFile)
    {
        var XML = XDocument.Load(sourceFile);
        var test = new XmlSerializer(typeof(Fxr3));
        XmlReader xmlReader = XML.CreateReader();

        var fxr = (Fxr3)test.Deserialize(xmlReader);

        var outPath = sourceFile.Replace(".fxr.xml", ".fxr");
        WBUtil.Backup(outPath);
        fxr.Write(outPath);

        return false;
    }
}