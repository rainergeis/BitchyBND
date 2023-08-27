using System.Collections.Generic;

namespace SoulsFormats;

public partial class MSB3
{
    /// <summary>
    ///     A list of names to be referenced by parts pose bones.
    /// </summary>
    private class MapstudioBoneName : Param<BoneName>
    {

        /// <summary>
        ///     Creates an empty MapstudioBoneName.
        /// </summary>
        public MapstudioBoneName()
        {
            Names = new List<BoneName>();
        }

        internal override int Version => 0;
        internal override string Type => "MAPSTUDIO_BONE_NAME_STRING";

        /// <summary>
        ///     All available names.
        /// </summary>
        public List<BoneName> Names { get; }

        /// <summary>
        ///     Returns every bone name in the order they will be written.
        /// </summary>
        public override List<BoneName> GetEntries()
        {
            return Names;
        }

        internal override BoneName ReadEntry(BinaryReaderEx br)
        {
            return Names.EchoAdd(new BoneName(br));
        }
    }

    /// <summary>
    ///     A single string for naming a bone.
    /// </summary>
    internal class BoneName : NamedEntry
    {

        /// <summary>
        ///     Creates a BoneName with default values.
        /// </summary>
        public BoneName()
        {
            Name = "Master";
        }

        internal BoneName(BinaryReaderEx br)
        {
            Name = br.ReadUTF16();
        }

        /// <summary>
        ///     The name of a bone.
        /// </summary>
        public override string Name { get; set; }

        /// <summary>
        ///     Creates a deep copy of the bone name.
        /// </summary>
        public BoneName DeepCopy()
        {
            return (BoneName)MemberwiseClone();
        }

        internal override void Write(BinaryWriterEx bw, int id)
        {
            bw.WriteUTF16(Name, true);
            bw.Pad(8);
        }
    }
}