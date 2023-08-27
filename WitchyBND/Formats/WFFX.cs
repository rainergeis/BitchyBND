using System.IO;
using SoulsFormats;

namespace WitchyBND;

internal static class WFFX
{
    public static void Unpack(this FFXDLSE ffx, string sourceFile)
    {
        using (var sw = new StreamWriter($"{sourceFile}.xml"))
        {
            ffx.XmlSerialize(sw);
        }
    }

    public static void Repack(string sourceFile)
    {
        FFXDLSE ffx;
        using (var sr = new StreamReader(sourceFile))
        {
            ffx = FFXDLSE.XmlDeserialize(sr);
        }

        var outPath = sourceFile.Replace(".ffx.xml", ".ffx").Replace(".ffx.dcx.xml", ".ffx.dcx");
        WBUtil.Backup(outPath);
        ffx.Write(outPath);
    }
}