using System.Collections.Generic;
using System.Numerics;

namespace SoulsFormats.KF4;

/// <summary>
///     A 3D model format used in King's Field IV. Extension: .om2
/// </summary>
public class OM2 : SoulsFile<OM2>
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public Struct1[] Struct1s { get; private set; }

    public List<Struct2> Struct2s { get; set; }

    protected override void Read(BinaryReaderEx br)
    {
        br.ReadInt32(); // File size
        var struct2Count = br.ReadInt16();
        br.ReadInt16(); // Mesh count
        br.ReadInt16(); // ? count
        br.ReadInt16(); // Vertex count
        br.AssertInt32(0);

        Struct1s = new Struct1[32];
        for (var i = 0; i < 32; i++)
            Struct1s[i] = new Struct1(br);

        Struct2s = new List<Struct2>(struct2Count);
        for (var i = 0; i < struct2Count; i++)
            Struct2s.Add(new Struct2(br));
    }

    public class Struct1
    {

        internal Struct1(BinaryReaderEx br)
        {
            Unk00 = br.ReadSingle();
            Unk04 = br.ReadSingle();
            Unk08 = br.ReadSingle();
            Unk0C = br.ReadByte();
            br.AssertByte(0);
            br.AssertByte(0);
            br.AssertByte(0);
        }

        public float Unk00 { get; set; }

        public float Unk04 { get; set; }

        public float Unk08 { get; set; }

        public byte Unk0C { get; set; }
    }

    public class Struct2
    {

        internal Struct2(BinaryReaderEx br)
        {
            var meshesOffset = br.ReadInt32();
            var meshCount = br.ReadInt16();
            Unk05 = br.ReadByte();
            Struct2Index = br.ReadByte();
            br.AssertInt32(0);
            br.AssertInt32(0);

            br.StepIn(meshesOffset);
            {
                Meshes = new List<Mesh>(meshCount);
                for (var i = 0; i < meshCount; i++)
                    Meshes.Add(new Mesh(br));
            }
            br.StepOut();
        }

        public List<Mesh> Meshes { get; set; }

        public byte Unk05 { get; set; }

        public byte Struct2Index { get; set; }
    }

    public class Mesh
    {

        internal Mesh(BinaryReaderEx br)
        {
            br.Skip(0xA0);
            var vertexCount = br.ReadByte();
            br.Skip(0xF);

            Vertices = new List<Vertex>(vertexCount);
            for (var i = 0; i < vertexCount; i++)
                Vertices.Add(new Vertex(br));

            br.Skip(0x10);
        }

        public List<Vertex> Vertices { get; set; }
    }

    public class Vertex
    {

        internal Vertex(BinaryReaderEx br)
        {
            Position = br.ReadVector3();
            Unk0C = br.ReadSingle();
            Normal = br.ReadVector3();
            Unk1C = br.ReadInt32();
            Unk20 = br.ReadVector3();
            Unk2C = br.ReadInt32();
            Unk30 = br.ReadVector4();
        }

        public Vector3 Position { get; set; }

        public float Unk0C { get; set; }

        public Vector3 Normal { get; set; }

        public int Unk1C { get; set; }

        public Vector3 Unk20 { get; set; }

        public int Unk2C { get; set; }

        public Vector4 Unk30 { get; set; }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}