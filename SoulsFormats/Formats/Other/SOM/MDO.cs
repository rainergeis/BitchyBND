using System.Collections.Generic;
using System.Numerics;

namespace SoulsFormats.SOM;

/// <summary>
///     A model format used in Sword of Moonlight for basic models like items.
/// </summary>
public class MDO : SoulsFile<MDO>
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public List<string> Textures;
    public List<Unk1> Unk1s;
    public List<Mesh> Meshes;

    protected override void Read(BinaryReaderEx br)
    {
        br.BigEndian = false;

        var textureCount = br.ReadInt32();
        Textures = new List<string>(textureCount);
        for (var i = 0; i < textureCount; i++)
            Textures.Add(br.ReadShiftJIS());
        br.Pad(4);

        var unk1Count = br.ReadInt32();
        Unk1s = new List<Unk1>(unk1Count);
        for (var i = 0; i < unk1Count; i++)
            Unk1s.Add(new Unk1(br));

        for (var i = 0; i < 12; i++)
            br.AssertInt32(0);

        var meshCount = br.ReadInt32();
        Meshes = new List<Mesh>(meshCount);
        for (var i = 0; i < meshCount; i++)
            Meshes.Add(new Mesh(br));
    }

    public class Unk1
    {
        public float Unk00, Unk04, Unk08, Unk0C, Unk10, Unk14, Unk18;

        internal Unk1(BinaryReaderEx br)
        {
            Unk00 = br.ReadSingle();
            Unk04 = br.ReadSingle();
            Unk08 = br.ReadSingle();
            Unk0C = br.ReadSingle();
            Unk10 = br.ReadSingle();
            Unk14 = br.ReadSingle();
            Unk18 = br.ReadSingle();
            br.AssertInt32(0);
        }
    }

    public class Mesh
    {
        public ushort[] Indices;
        public short TextureIndex;
        public int Unk00;
        public short Unk06;
        public List<Vertex> Vertices;

        internal Mesh(BinaryReaderEx br)
        {
            Unk00 = br.ReadInt32();
            TextureIndex = br.ReadInt16();
            Unk06 = br.ReadInt16();
            var indexCount = br.ReadUInt16();
            var vertexCount = br.ReadUInt16();
            var indicesOffset = br.ReadUInt32();
            var verticesOffset = br.ReadUInt32();

            Indices = br.GetUInt16s(indicesOffset, indexCount);

            br.StepIn(verticesOffset);
            {
                Vertices = new List<Vertex>(vertexCount);
                for (var i = 0; i < vertexCount; i++)
                    Vertices.Add(new Vertex(br));
            }
            br.StepOut();
        }

        public List<Vertex[]> GetFaces()
        {
            var faces = new List<Vertex[]>();
            for (var i = 0; i < Indices.Length; i += 3)
                faces.Add(new[]
                {
                    Vertices[Indices[i + 0]],
                    Vertices[Indices[i + 1]],
                    Vertices[Indices[i + 2]]
                });
            return faces;
        }
    }

    public class Vertex
    {
        public Vector3 Normal;
        public Vector3 Position;
        public Vector2 UV;

        internal Vertex(BinaryReaderEx br)
        {
            Position = br.ReadVector3();
            Normal = br.ReadVector3();
            UV = br.ReadVector2();
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}