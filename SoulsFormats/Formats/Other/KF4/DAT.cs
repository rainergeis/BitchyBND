using System.Collections.Generic;

namespace SoulsFormats.KF4;

/// <summary>
///     Specifically KF4.DAT, the main archive.
/// </summary>
public class DAT : SoulsFile<DAT>
{
    /// <summary>
    ///     Files in the archive.
    /// </summary>
    public List<File> Files;

    /// <summary>
    ///     Deserializes file data from a stream.
    /// </summary>
    protected override void Read(BinaryReaderEx br)
    {
        br.BigEndian = false;

        br.AssertByte(0x00);
        br.AssertByte(0x80);
        br.AssertByte(0x04);
        br.AssertByte(0x1E);

        var fileCount = br.ReadInt32();

        for (var i = 0; i < 0x38; i++)
            br.AssertByte(0);

        Files = new List<File>(fileCount);
        for (var i = 0; i < fileCount; i++)
            Files.Add(new File(br));
    }

    /// <summary>
    ///     A file in a DAT archive.
    /// </summary>
    public class File
    {

        /// <summary>
        ///     The file's data.
        /// </summary>
        public byte[] Bytes;

        /// <summary>
        ///     The path of the file.
        /// </summary>
        public string Name;

        internal File(BinaryReaderEx br)
        {
            Name = br.ReadFixStr(0x34);
            var size = br.ReadInt32();
            var paddedSize = br.ReadInt32();
            var offset = br.ReadInt32();

            Bytes = br.GetBytes(offset, size);
        }
    }
}