using System.Collections.Generic;

namespace SoulsFormats;

public partial class MSBD
{
    private class MapstudioTree : Param<Tree>
    {

        public MapstudioTree()
        {
            Trees = new List<Tree>();
        }

        internal override string Name => "MAPSTUDIO_TREE_ST";

        public List<Tree> Trees { get; }

        internal override Tree ReadEntry(BinaryReaderEx br)
        {
            return Trees.EchoAdd(new Tree(br));
        }

        public override List<Tree> GetEntries()
        {
            return Trees;
        }
    }

    /// <summary>
    ///     Unknown.
    /// </summary>
    public class Tree : Entry
    {

        /// <summary>
        ///     Creates a Tree with default values.
        /// </summary>
        public Tree()
        {
            UnkShorts = new List<short>();
        }

        internal Tree(BinaryReaderEx br)
        {
            Unk00 = br.ReadSingle();
            Unk04 = br.ReadSingle();
            Unk08 = br.ReadSingle();
            Unk0C = br.ReadInt32();
            Unk10 = br.ReadSingle();
            Unk14 = br.ReadSingle();
            Unk18 = br.ReadSingle();
            Unk1C = br.ReadInt32();
            Unk20 = br.ReadSingle();
            Unk24 = br.ReadSingle();
            Unk28 = br.ReadSingle();
            Unk2C = br.ReadInt32();
            Unk30 = br.ReadSingle();
            var shortCount = br.ReadInt32();
            UnkShorts = new List<short>(br.ReadInt16s(shortCount));
        }

        /// <summary>
        ///     Unknown.
        /// </summary>
        public float Unk00 { get; set; }

        /// <summary>
        ///     Unknown.
        /// </summary>
        public float Unk04 { get; set; }

        /// <summary>
        ///     Unknown.
        /// </summary>
        public float Unk08 { get; set; }

        /// <summary>
        ///     Unknown.
        /// </summary>
        public int Unk0C { get; set; }

        /// <summary>
        ///     Unknown.
        /// </summary>
        public float Unk10 { get; set; }

        /// <summary>
        ///     Unknown.
        /// </summary>
        public float Unk14 { get; set; }

        /// <summary>
        ///     Unknown.
        /// </summary>
        public float Unk18 { get; set; }

        /// <summary>
        ///     Unknown.
        /// </summary>
        public int Unk1C { get; set; }

        /// <summary>
        ///     Unknown.
        /// </summary>
        public float Unk20 { get; set; }

        /// <summary>
        ///     Unknown.
        /// </summary>
        public float Unk24 { get; set; }

        /// <summary>
        ///     Unknown.
        /// </summary>
        public float Unk28 { get; set; }

        /// <summary>
        ///     Unknown.
        /// </summary>
        public int Unk2C { get; set; }

        /// <summary>
        ///     Unknown.
        /// </summary>
        public float Unk30 { get; set; }

        /// <summary>
        ///     Unknown.
        /// </summary>
        public List<short> UnkShorts { get; set; }

        /// <summary>
        ///     Creates a deep copy of the tree.
        /// </summary>
        public Tree DeepCopy()
        {
            var tree = (Tree)MemberwiseClone();
            tree.UnkShorts = new List<short>(UnkShorts);
            return tree;
        }

        internal override void Write(BinaryWriterEx bw, int id)
        {
            bw.WriteSingle(Unk00);
            bw.WriteSingle(Unk04);
            bw.WriteSingle(Unk08);
            bw.WriteInt32(Unk0C);
            bw.WriteSingle(Unk10);
            bw.WriteSingle(Unk14);
            bw.WriteSingle(Unk18);
            bw.WriteInt32(Unk1C);
            bw.WriteSingle(Unk20);
            bw.WriteSingle(Unk24);
            bw.WriteSingle(Unk28);
            bw.WriteInt32(Unk2C);
            bw.WriteSingle(Unk30);
            bw.WriteInt32(UnkShorts.Count);
            bw.WriteInt16s(UnkShorts);
            bw.Pad(0x10);
        }
    }
}