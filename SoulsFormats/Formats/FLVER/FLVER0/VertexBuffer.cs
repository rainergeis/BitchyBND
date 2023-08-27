using System.Collections.Generic;

namespace SoulsFormats;

public partial class FLVER0
{
    private class VertexBuffer
    {

        public int BufferLength;

        public int BufferOffset;
        public int LayoutIndex;

        public VertexBuffer()
        {
        }

        internal VertexBuffer(BinaryReaderEx br)
        {
            LayoutIndex = br.ReadInt32();
            BufferLength = br.ReadInt32();
            BufferOffset = br.ReadInt32();
            br.AssertInt32(0);
        }

        internal static List<VertexBuffer> ReadVertexBuffers(BinaryReaderEx br)
        {
            var bufferCount = br.ReadInt32();
            var buffersOffset = br.ReadInt32();
            br.AssertInt32(0);
            br.AssertInt32(0);

            var buffers = new List<VertexBuffer>(bufferCount);
            br.StepIn(buffersOffset);
            {
                for (var i = 0; i < bufferCount; i++)
                    buffers.Add(new VertexBuffer(br));
            }
            br.StepOut();
            return buffers;
        }
    }
}