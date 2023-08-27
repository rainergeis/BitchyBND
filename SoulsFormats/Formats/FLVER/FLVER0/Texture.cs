namespace SoulsFormats;

public partial class FLVER0
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class Texture : IFlverTexture
    {

        internal Texture(BinaryReaderEx br, FLVER0 flv)
        {
            var pathOffset = br.ReadInt32();
            var typeOffset = br.ReadInt32();
            br.AssertInt32(0);
            br.AssertInt32(0);

            Path = flv.Unicode ? br.GetUTF16(pathOffset) : br.GetShiftJIS(pathOffset);
            if (typeOffset > 0)
                Type = flv.Unicode ? br.GetUTF16(typeOffset) : br.GetShiftJIS(typeOffset);
            else
                Type = null;
        }

        public string Type { get; set; }

        public string Path { get; set; }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}