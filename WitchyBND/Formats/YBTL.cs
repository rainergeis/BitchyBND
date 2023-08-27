using System;
using System.IO;
using SoulsFormats;
using WitchyBND;

namespace Yabber;

internal static class YBTL
{
    public static void Unpack(this BTL btl, string sourceFile)
    {
        var targetFile = $"{sourceFile}.json";

        if (File.Exists(targetFile)) WBUtil.Backup(targetFile);

        File.WriteAllText(targetFile, WBUtil.JsonSerialize(btl));
    }

    public static void Repack(string sourceFile)
    {
        string outPath;
        if (sourceFile.EndsWith(".btl.json"))
            outPath = sourceFile.Replace(".btl.json", ".btl");
        else if (sourceFile.EndsWith(".btl.dcx.json"))
            outPath = sourceFile.Replace(".btl.dcx.json", ".btl.dcx");
        else
            throw new InvalidOperationException("Invalid BTL json filename.");

        if (File.Exists(outPath)) WBUtil.Backup(outPath);

        WBUtil.JsonDeserialize<BTL>(File.ReadAllText(sourceFile)).Write(outPath);
    }
}