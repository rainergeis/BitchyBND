using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SoulsFormats;

public partial class FLVER0
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class BufferLayout : List<FLVER.LayoutMember>
    {

        internal BufferLayout(BinaryReaderEx br)
        {
            var memberCount = br.ReadInt16();
            var structSize = br.ReadInt16();
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);

            var structOffset = 0;
            Capacity = memberCount;
            for (var i = 0; i < memberCount; i++)
            {
                var member = new FLVER.LayoutMember(br, structOffset);
                structOffset += member.Size;
                Add(member);
            }

            if (Size != structSize)
                throw new InvalidDataException("Mismatched buffer layout size.");
        }

        /// <summary>
        ///     The total size of all ValueTypes in this layout.
        /// </summary>
        public int Size => this.Sum(member => member.Size);
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}