using System.Collections.Generic;

namespace SoulsFormats.KF4;

/// <summary>
///     A map asset container used in King's Field IV. Extension: .map
/// </summary>
public class MAP : SoulsFile<MAP>
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public List<Struct4> Struct4s { get; set; }

    protected override void Read(BinaryReaderEx br)
    {
        br.ReadInt32(); // File size
        var offset1 = br.ReadInt32();
        var offset2 = br.ReadInt32();
        var offset3 = br.ReadInt32();
        var offset4 = br.ReadInt32();
        var offset5 = br.ReadInt32();
        var offset6 = br.ReadInt32();
        br.AssertInt32(0);
        var offset8 = br.ReadInt32();
        var offset9 = br.ReadInt32();
        var offset10 = br.ReadInt32();
        var count1 = br.ReadInt16();
        var count2 = br.ReadInt16();
        var count3 = br.ReadInt16();
        var count4 = br.ReadInt16();
        var count5 = br.ReadInt16();
        var count6 = br.ReadInt16();
        br.AssertInt16(0);
        var count8 = br.ReadInt16();
        var count9 = br.ReadInt16();
        var count10 = br.ReadInt16();

        br.Position = offset4;
        Struct4s = new List<Struct4>(count4);
        for (var i = 0; i < count4; i++)
            Struct4s.Add(new Struct4(br));
    }

    public class Struct4
    {

        internal Struct4(BinaryReaderEx br)
        {
            var om2Bytes = br.ReadBytes(br.GetInt32(br.Position));
            var tx2Bytes = br.ReadBytes(br.GetInt32(br.Position));

            Om2 = OM2.Read(om2Bytes);
        }

        public OM2 Om2 { get; set; }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}