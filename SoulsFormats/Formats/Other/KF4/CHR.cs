namespace SoulsFormats.KF4;

/// <summary>
///     Container for character files in King's Field IV. Extension: .chr
/// </summary>
public class CHR : SoulsFile<CHR>
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public int Unk00 { get; set; }

    public OM2 Om2 { get; set; }

    protected override void Read(BinaryReaderEx br)
    {
        Unk00 = br.ReadInt32();
        var om2Offset = br.ReadInt32();
        var mixOffset = br.ReadInt32();
        var tx2Offset = br.ReadInt32();
        var hdOffset = br.ReadInt32();
        var bdOffset = br.ReadInt32();
        var bdLength = br.ReadInt32();

        if (om2Offset != 0)
        {
            br.Position = om2Offset;
            var om2Bytes = br.ReadBytes(br.GetInt32(br.Position));
            Om2 = OM2.Read(om2Bytes);
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}