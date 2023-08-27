using System.Collections.Generic;
using System.Linq;

namespace SoulsFormats;

public partial class FLVER2
{
    /// <summary>
    ///     Determines which properties of a vertex are read and written, and in what order and format.
    /// </summary>
    public class BufferLayout : List<FLVER.LayoutMember>
    {

        /// <summary>
        ///     Creates a new empty BufferLayout.
        /// </summary>
        public BufferLayout()
        {
        }

        internal BufferLayout(BinaryReaderEx br)
        {
            var memberCount = br.ReadInt32();
            br.AssertInt32(0);
            br.AssertInt32(0);
            var memberOffset = br.ReadInt32();

            br.StepIn(memberOffset);
            {
                var structOffset = 0;
                Capacity = memberCount;
                for (var i = 0; i < memberCount; i++)
                {
                    var member = new FLVER.LayoutMember(br, structOffset);
                    structOffset += member.Size;
                    Add(member);
                }
            }
            br.StepOut();
        }

        /// <summary>
        ///     The total size of all ValueTypes in this layout.
        /// </summary>
        public int Size => this.Sum(member => member.Size);

        internal void Write(BinaryWriterEx bw, int index)
        {
            bw.WriteInt32(Count);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.ReserveInt32($"VertexStructLayout{index}");
        }

        internal void WriteMembers(BinaryWriterEx bw, int index)
        {
            bw.FillInt32($"VertexStructLayout{index}", (int)bw.Position);
            var structOffset = 0;
            foreach (FLVER.LayoutMember member in this)
            {
                member.Write(bw, structOffset);
                structOffset += member.Size;
            }
        }
    }
}