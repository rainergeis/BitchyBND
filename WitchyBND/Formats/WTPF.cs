﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using SoulsFormats;
using WitchyFormats.Utils;
using TPF = WitchyFormats.TPF;

namespace WitchyBND;

internal static class WTPF
{
    private static readonly List<TPF.TPFPlatform> supportedPlatforms =
        new() { TPF.TPFPlatform.PC, TPF.TPFPlatform.PS3, TPF.TPFPlatform.PS4 };

    public static bool Unpack(this TPF tpf, string sourceName, string targetDir, IProgress<float> progress)
    {
        if (!supportedPlatforms.Contains(tpf.Platform))
            Console.WriteLine(
                @"WitchyBND currently only supports unpacking PC, PS3 and PS4 TPFs. There may be issues with other console TPFs.");

        Directory.CreateDirectory(targetDir);
        var xws = new XmlWriterSettings();
        xws.Indent = true;
        var xw = XmlWriter.Create($"{targetDir}\\_witchy-tpf.xml", xws);
        xw.WriteStartElement("tpf");

        xw.WriteElementString("filename", sourceName);
        xw.WriteElementString("compression", tpf.Compression.ToString());
        xw.WriteElementString("encoding", $"0x{tpf.Encoding:X2}");
        xw.WriteElementString("flag2", $"0x{tpf.Flag2:X2}");
        xw.WriteElementString("platform", tpf.Platform.ToString());

        xw.WriteStartElement("textures");
        for (var i = 0; i < tpf.Textures.Count; i++)
        {
            TPF.Texture texture = tpf.Textures[i];
            xw.WriteStartElement("texture");
            xw.WriteElementString("name", texture.Name + ".dds");
            xw.WriteElementString("format", texture.Format.ToString());
            xw.WriteElementString("flags1", $"0x{texture.Flags1:X2}");

            if (texture.FloatStruct != null)
            {
                xw.WriteStartElement("FloatStruct");
                xw.WriteAttributeString("Unk00", texture.FloatStruct.Unk00.ToString());
                foreach (var value in texture.FloatStruct.Values) xw.WriteElementString("Value", value.ToString());

                xw.WriteEndElement();
            }

            xw.WriteEndElement();

            try
            {
                File.WriteAllBytes($"{targetDir}\\{texture.Name}.dds", texture.Headerize());
            }
            catch (EndOfStreamException)
            {
                try
                {
                    File.WriteAllBytes($"{targetDir}\\{texture.Name}.dds",
                        SecretHeaderizer.SecretHeaderize(texture));
                }
                catch (Exception e)
                {
                    Console.WriteLine(@"There was an error unpacking the TPF:");
                    Console.WriteLine(e);
                    return true;
                }
            }

            progress.Report((float)i / tpf.Textures.Count);
        }

        xw.WriteEndElement();

        xw.WriteEndElement();
        xw.Close();

        return false;
    }

    public static bool Repack(string sourceDir, string targetDir)
    {
        var tpf = new TPF();
        var xml = new XmlDocument();

        xml.Load(WBUtil.GetXmlPath("tpf", sourceDir));

        Enum.TryParse(xml.SelectSingleNode("tpf/platform")?.InnerText ?? "None", out TPF.TPFPlatform platform);
        tpf.Platform = platform;

        var filename = xml.SelectSingleNode("tpf/filename").InnerText;
        Enum.TryParse(xml.SelectSingleNode("tpf/compression")?.InnerText ?? "None", out DCX.Type compression);
        tpf.Compression = compression;

        tpf.Encoding = Convert.ToByte(xml.SelectSingleNode("tpf/encoding").InnerText, 16);
        tpf.Flag2 = Convert.ToByte(xml.SelectSingleNode("tpf/flag2").InnerText, 16);

        foreach (XmlNode texNode in xml.SelectNodes("tpf/textures/texture"))
        {
            var name = Path.GetFileNameWithoutExtension(texNode.SelectSingleNode("name").InnerText);
            var format = Convert.ToByte(texNode.SelectSingleNode("format").InnerText);
            var flags1 = Convert.ToByte(texNode.SelectSingleNode("flags1").InnerText, 16);

            TPF.FloatStruct floatStruct = null;
            XmlNode floatsNode = texNode.SelectSingleNode("FloatStruct");
            if (floatsNode != null)
            {
                floatStruct = new TPF.FloatStruct();
                floatStruct.Unk00 = int.Parse(floatsNode.Attributes["Unk00"].InnerText);
                foreach (XmlNode valueNode in floatsNode.SelectNodes("Value"))
                    floatStruct.Values.Add(float.Parse(valueNode.InnerText));
            }

            var bytes = File.ReadAllBytes($"{sourceDir}\\{name}.dds");
            var texture = new TPF.Texture(name, format, flags1, bytes);
            texture.FloatStruct = floatStruct;
            tpf.Textures.Add(texture);
        }

        var outPath = $"{targetDir}\\{filename}";
        WBUtil.Backup(outPath);
        try
        {
            tpf.Write(outPath);
        }
        catch (Exception e)
        {
            if (platform != TPF.TPFPlatform.PC)
            {
                Console.WriteLine("Writing TPF failed.");
                Console.WriteLine(
                    @"WitchyBND only officially supports repacking PC TPFs at the moment. Repacking console TPFs is not supported.");
                return true;
            }

            throw new Exception("Error while writing TPF", e);
        }

        return false;
    }
}