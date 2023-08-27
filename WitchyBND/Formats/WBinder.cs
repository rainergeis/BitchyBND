using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using SoulsFormats;

namespace WitchyBND;

internal static class WBinder
{
    public static void WriteBinderFiles(BinderReader bnd, XmlWriter xw, string targetDir, IProgress<float> progress)
    {
        var root = "";
        if (Binder.HasNames(bnd.Format))
        {
            root = WBUtil.FindCommonRootPath(bnd.Files.Select(bndFile => bndFile.Name));

            if (root != "")
                // If there is a common root path, add it to the XML so it can be used in repacking.
                xw.WriteElementString("root", root + "\\");
        }

        xw.WriteStartElement("files");
        var pathCounts = new Dictionary<string, int>();

        for (var i = 0; i < bnd.Files.Count; i++)
        {
            BinderFileHeader file = bnd.Files[i];

            string path;
            if (Binder.HasNames(bnd.Format))
                path = WBUtil.UnrootBNDPath(file.Name, root);
            else if (Binder.HasIDs(bnd.Format))
                path = file.ID.ToString();
            else
                path = i.ToString();

            xw.WriteStartElement("file");
            xw.WriteElementString("flags", file.Flags.ToString());

            if (Binder.HasIDs(bnd.Format))
                xw.WriteElementString("id", file.ID.ToString());

            xw.WriteElementString("path", path);

            var suffix = "";
            if (pathCounts.ContainsKey(path))
            {
                pathCounts[path]++;
                suffix = $" ({pathCounts[path]})";
                xw.WriteElementString("suffix", suffix);
            }
            else
            {
                pathCounts[path] = 1;
            }

            if (file.CompressionType != DCX.Type.Zlib)
                xw.WriteElementString("compression_type", file.CompressionType.ToString());

            xw.WriteEndElement();

            var bytes = bnd.ReadFile(file);
            var outPath =
                $@"{targetDir}\{Path.GetDirectoryName(path)}\{Path.GetFileNameWithoutExtension(path)}{suffix}{Path.GetExtension(path)}";
            Directory.CreateDirectory(Path.GetDirectoryName(outPath));
            File.WriteAllBytes(outPath, bytes);
            progress.Report((float)i / bnd.Files.Count);
        }

        xw.WriteEndElement();
    }

    public static void ReadBinderFiles(IBinder bnd, XmlNode filesNode, string sourceDir, string root)
    {
        foreach (XmlNode fileNode in filesNode.SelectNodes("file"))
        {
            if (fileNode.SelectSingleNode("path") == null)
                throw new FriendlyException("File node missing path tag.");

            var strFlags = fileNode.SelectSingleNode("flags")?.InnerText ?? "Flag1";
            var strID = fileNode.SelectSingleNode("id")?.InnerText ?? "-1";
            var path = fileNode.SelectSingleNode("path").InnerText;
            var suffix = fileNode.SelectSingleNode("suffix")?.InnerText ?? "";
            var strCompression = fileNode.SelectSingleNode("compression_type")?.InnerText ?? DCX.Type.Zlib.ToString();
            var name = root + path;

            if (!Enum.TryParse(strFlags, out Binder.FileFlags flags))
                throw new FriendlyException(
                    $"Could not parse file flags: {strFlags}\nFlags must be comma-separated list of flags.");

            if (!int.TryParse(strID, out var id))
                throw new FriendlyException($"Could not parse file ID: {strID}\nID must be a 32-bit signed integer.");

            if (!Enum.TryParse(strCompression, out DCX.Type compressionType))
                throw new FriendlyException(
                    $"Could not parse compression type: {strCompression}\nCompression type must be a valid DCX Type.");

            var inPath =
                $@"{sourceDir}\{Path.GetDirectoryName(path)}\{Path.GetFileNameWithoutExtension(path)}{suffix}{Path.GetExtension(path)}";
            if (!File.Exists(inPath))
                throw new FriendlyException($"File not found: {inPath}");

            var bytes = File.ReadAllBytes(inPath);
            bnd.Files.Add(new BinderFile(flags, id, name, bytes)
            {
                CompressionType = compressionType
            });
        }
    }
}