using System.Collections.Generic;

namespace SoulsFormats.Kuon;

/// <summary>
///     Most BNDs inside ALL/ELL. Extension: .bnd
/// </summary>
public class BND0 : SoulsFile<BND0>
{
    /// <summary>
    ///     Files in this BND.
    /// </summary>
    public List<File> Files;

    /// <summary>
    ///     Unknown; 0xC8 or 0xCA.
    /// </summary>
    public int Unk04;

    /// <summary>
    ///     Checks whether the data appears to be a file of this format.
    /// </summary>
    protected override bool Is(BinaryReaderEx br)
    {
        if (br.Length < 4)
            return false;

        var magic = br.GetASCII(0, 4);
        return magic == "BND\0";
    }

    /// <summary>
    ///     Deserializes file data from a stream.
    /// </summary>
    protected override void Read(BinaryReaderEx br)
    {
        br.BigEndian = false;

        br.AssertASCII("BND\0");
        Unk04 = br.AssertInt32(0xC8, 0xCA);
        var fileSize = br.ReadInt32();
        var fileCount = br.ReadInt32();

        Files = new List<File>(fileCount);
        for (var i = 0; i < fileCount; i++)
        {
            var nextOffset = fileSize;
            if (i < fileCount - 1)
                nextOffset = br.GetInt32(br.Position + 0xC + 4);
            Files.Add(new File(br, nextOffset));
        }
    }

    /// <summary>
    ///     A file in a BND0.
    /// </summary>
    public class File
    {

        /// <summary>
        ///     File data.
        /// </summary>
        public byte[] Bytes;

        /// <summary>
        ///     ID of this file.
        /// </summary>
        public int ID;

        /// <summary>
        ///     Name of this file.
        /// </summary>
        public string Name;

        internal File(BinaryReaderEx br, int nextOffset)
        {
            ID = br.ReadInt32();
            var dataOffset = br.ReadInt32();
            var nameOffset = br.ReadInt32();

            Name = br.GetShiftJIS(nameOffset);
            Bytes = br.GetBytes(dataOffset, nextOffset - dataOffset);
        }
    }
}