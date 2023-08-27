using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;

namespace SoulsFormats.Other;

/// <summary>
///     A 3D model format used in early PS3/X360 games. Extension: .mdl
/// </summary>
public class MDL4 : SoulsFile<MDL4>
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public int Version;
    public int Unk20;
    public Vector3 BoundingBoxMin;
    public Vector3 BoundingBoxMax;
    public int TrueFaceCount;
    public int TotalFaceCount;

    public List<Dummy> Dummies;
    public List<Material> Materials;
    public List<Bone> Bones;
    public List<Mesh> Meshes;

    protected override bool Is(BinaryReaderEx br)
    {
        if (br.Length < 4)
            return false;

        var magic = br.GetASCII(0, 4);
        return magic == "MDL4";
    }

    protected override void Read(BinaryReaderEx br)
    {
        br.BigEndian = true;
        br.AssertASCII("MDL4");
        Version = br.AssertInt32(0x40001, 0x40002);
        var dataStart = br.ReadInt32();
        br.ReadInt32(); // Data length
        var dummyCount = br.ReadInt32();
        var materialCount = br.ReadInt32();
        var boneCount = br.ReadInt32();
        var meshCount = br.ReadInt32();
        Unk20 = br.ReadInt32();
        BoundingBoxMin = br.ReadVector3();
        BoundingBoxMax = br.ReadVector3();
        TrueFaceCount = br.ReadInt32();
        TotalFaceCount = br.ReadInt32();
        br.AssertPattern(0x3C, 0x00);

        Dummies = new List<Dummy>(dummyCount);
        for (var i = 0; i < dummyCount; i++)
            Dummies.Add(new Dummy(br));

        Materials = new List<Material>(materialCount);
        for (var i = 0; i < materialCount; i++)
            Materials.Add(new Material(br));

        Bones = new List<Bone>(boneCount);
        for (var i = 0; i < boneCount; i++)
            Bones.Add(new Bone(br));

        Meshes = new List<Mesh>(meshCount);
        for (var i = 0; i < meshCount; i++)
            Meshes.Add(new Mesh(br, dataStart, Version));
    }

    public class Dummy
    {
        public Color Color;
        public Vector3 Forward;
        public short ID;
        public short Unk1E;
        public short Unk20;
        public short Unk22;
        public Vector3 Upward;

        internal Dummy(BinaryReaderEx br)
        {
            Forward = br.ReadVector3();
            Upward = br.ReadVector3();
            Color = br.ReadRGBA();
            ID = br.ReadInt16();
            Unk1E = br.ReadInt16();
            Unk20 = br.ReadInt16();
            Unk22 = br.ReadInt16();
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
        }
    }

    public class Material
    {

        public enum ParamType : byte
        {
            Int = 0,
            Float = 1,
            Float4 = 4,
            String = 5
        }

        public string Name;
        public List<Param> Params;
        public string Shader;
        public byte Unk3C;
        public byte Unk3D;
        public byte Unk3E;

        internal Material(BinaryReaderEx br)
        {
            Name = br.ReadFixStr(0x1F);
            Shader = br.ReadFixStr(0x1D);
            Unk3C = br.ReadByte();
            Unk3D = br.ReadByte();
            Unk3E = br.ReadByte();
            var paramCount = br.ReadByte();

            var paramsStart = br.Position;
            Params = new List<Param>(paramCount);
            for (var i = 0; i < paramCount; i++)
                Params.Add(new Param(br));
            br.Position = paramsStart + 0x800;
        }

        public class Param
        {
            public string Name;
            public ParamType Type;
            public object Value;

            internal Param(BinaryReaderEx br)
            {
                var start = br.Position;
                Type = br.ReadEnum8<ParamType>();
                Name = br.ReadFixStr(0x1F);

                switch (Type)
                {
                    case ParamType.Int:
                        Value = br.ReadInt32();
                        break;
                    case ParamType.Float:
                        Value = br.ReadSingle();
                        break;
                    case ParamType.Float4:
                        Value = br.ReadSingles(4);
                        break;
                    case ParamType.String:
                        Value = br.ReadShiftJIS();
                        break;

                    default:
                        throw new NotImplementedException("Unknown param type: " + Type);
                }

                br.Position = start + 0x40;
            }
        }
    }

    public class Bone
    {
        public Vector3 BoundingBoxMax;
        public Vector3 BoundingBoxMin;
        public short ChildIndex;
        public string Name;
        public short NextSiblingIndex;
        public short ParentIndex;
        public short PreviousSiblingIndex;
        public Vector3 Rotation;
        public Vector3 Scale;
        public Vector3 Translation;
        public short[] UnkIndices;

        internal Bone(BinaryReaderEx br)
        {
            Name = br.ReadFixStr(0x20);
            Translation = br.ReadVector3();
            Rotation = br.ReadVector3();
            Scale = br.ReadVector3();
            BoundingBoxMin = br.ReadVector3();
            BoundingBoxMax = br.ReadVector3();
            ParentIndex = br.ReadInt16();
            ChildIndex = br.ReadInt16();
            NextSiblingIndex = br.ReadInt16();
            PreviousSiblingIndex = br.ReadInt16();
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            UnkIndices = br.ReadInt16s(16);
        }

        /// <summary>
        ///     Creates a transformation matrix from the scale, rotation, and translation of the bone.
        /// </summary>
        public Matrix4x4 ComputeLocalTransform()
        {
            return Matrix4x4.CreateScale(Scale)
                   * Matrix4x4.CreateRotationX(Rotation.X)
                   * Matrix4x4.CreateRotationZ(Rotation.Z)
                   * Matrix4x4.CreateRotationY(Rotation.Y)
                   * Matrix4x4.CreateTranslation(Translation);
        }
    }

    public class Mesh
    {
        public short[] BoneIndices;
        public byte MaterialIndex;
        public bool Unk02;
        public bool Unk03;
        public short Unk08;
        public byte[][] UnkBlocks;
        public byte VertexFormat;
        public ushort[] VertexIndices;
        public List<Vertex> Vertices;

        internal Mesh(BinaryReaderEx br, int dataStart, int version)
        {
            VertexFormat = br.AssertByte(0, 1, 2);
            MaterialIndex = br.ReadByte();
            Unk02 = br.ReadBoolean();
            Unk03 = br.ReadBoolean();
            var vertexIndexCount = br.ReadUInt16();
            Unk08 = br.ReadInt16();
            BoneIndices = br.ReadInt16s(28);
            br.ReadInt32(); // Vertex indices length
            var vertexIndicesOffset = br.ReadInt32();
            var bufferLength = br.ReadInt32();
            var bufferOffset = br.ReadInt32();

            if (VertexFormat == 2)
            {
                UnkBlocks = new byte[16][];
                for (var i = 0; i < 16; i++)
                {
                    var length = br.ReadInt32();
                    var offset = br.ReadInt32();
                    UnkBlocks[i] = br.GetBytes(dataStart + offset, length);
                }
            }

            VertexIndices = br.GetUInt16s(dataStart + vertexIndicesOffset, vertexIndexCount);

            br.StepIn(dataStart + bufferOffset);
            {
                var vertexSize = 0;
                if (version == 0x40001)
                {
                    if (VertexFormat == 0)
                        vertexSize = 0x40;
                    else if (VertexFormat == 1)
                        vertexSize = 0x54;
                    else if (VertexFormat == 2)
                        vertexSize = 0x3C;
                }
                else if (version == 0x40002)
                {
                    if (VertexFormat == 0)
                        vertexSize = 0x28;
                }

                var vertexCount = bufferLength / vertexSize;
                Vertices = new List<Vertex>(vertexCount);
                for (var i = 0; i < vertexCount; i++)
                    Vertices.Add(new Vertex(br, version, VertexFormat));
            }
            br.StepOut();
        }

        public List<Vertex[]> GetFaces()
        {
            var indices = ToTriangleList();
            var faces = new List<Vertex[]>();
            for (var i = 0; i < indices.Length; i += 3)
                faces.Add(new[]
                {
                    Vertices[indices[i + 0]],
                    Vertices[indices[i + 1]],
                    Vertices[indices[i + 2]]
                });
            return faces;
        }

        public ushort[] ToTriangleList()
        {
            var converted = new List<ushort>();
            var flip = false;
            for (var i = 0; i < VertexIndices.Length - 2; i++)
            {
                var vi1 = VertexIndices[i];
                var vi2 = VertexIndices[i + 1];
                var vi3 = VertexIndices[i + 2];

                if (vi1 == 0xFFFF || vi2 == 0xFFFF || vi3 == 0xFFFF)
                {
                    flip = false;
                }
                else
                {
                    if (vi1 != vi2 && vi1 != vi3 && vi2 != vi3)
                    {
                        if (!flip)
                        {
                            converted.Add(vi1);
                            converted.Add(vi2);
                            converted.Add(vi3);
                        }
                        else
                        {
                            converted.Add(vi3);
                            converted.Add(vi2);
                            converted.Add(vi1);
                        }
                    }

                    flip = !flip;
                }
            }

            return converted.ToArray();
        }
    }

    public class Vertex
    {
        public Vector4 Bitangent;
        public short[] BoneIndices;
        public float[] BoneWeights;
        public byte[] Color;
        public Vector4 Normal;
        public Vector3 Position;
        public Vector4 Tangent;
        public short Unk3C;
        public short Unk3E;
        public List<Vector2> UVs;

        internal Vertex(BinaryReaderEx br, int version, byte format)
        {
            UVs = new List<Vector2>();
            if (version == 0x40001)
            {
                if (format == 0)
                {
                    Position = br.ReadVector3();
                    Normal = Read10BitVector4(br);
                    Tangent = Read10BitVector4(br);
                    Bitangent = Read10BitVector4(br);
                    Color = br.ReadBytes(4);
                    UVs.Add(br.ReadVector2());
                    UVs.Add(br.ReadVector2());
                    UVs.Add(br.ReadVector2());
                    UVs.Add(br.ReadVector2());
                    Unk3C = br.ReadInt16();
                    Unk3E = br.AssertInt16(0);
                }
                else if (format == 1)
                {
                    Position = br.ReadVector3();
                    Normal = Read10BitVector4(br);
                    Tangent = Read10BitVector4(br);
                    Bitangent = Read10BitVector4(br);
                    Color = br.ReadBytes(4);
                    UVs.Add(br.ReadVector2());
                    UVs.Add(br.ReadVector2());
                    UVs.Add(br.ReadVector2());
                    UVs.Add(br.ReadVector2());
                    BoneIndices = br.ReadInt16s(4);
                    BoneWeights = br.ReadSingles(4);
                }
                else if (format == 2)
                {
                    Color = br.ReadBytes(4);
                    UVs.Add(br.ReadVector2());
                    UVs.Add(br.ReadVector2());
                    UVs.Add(br.ReadVector2());
                    UVs.Add(br.ReadVector2());
                    BoneIndices = br.ReadInt16s(4);
                    BoneWeights = br.ReadSingles(4);
                }
            }
            else if (version == 0x40002)
            {
                if (format == 0)
                {
                    Position = br.ReadVector3();
                    Normal = ReadSByteVector4(br);
                    Tangent = ReadSByteVector4(br);
                    Color = br.ReadBytes(4);
                    UVs.Add(ReadShortUV(br));
                    UVs.Add(ReadShortUV(br));
                    UVs.Add(ReadShortUV(br));
                    UVs.Add(ReadShortUV(br));
                }
            }
        }

        private static Vector4 ReadByteVector4(BinaryReaderEx br)
        {
            var w = br.ReadByte();
            var z = br.ReadByte();
            var y = br.ReadByte();
            var x = br.ReadByte();
            return new Vector4((x - 127) / 127f, (y - 127) / 127f, (z - 127) / 127f, (w - 127) / 127f);
        }

        private static Vector4 ReadSByteVector4(BinaryReaderEx br)
        {
            var w = br.ReadSByte();
            var z = br.ReadSByte();
            var y = br.ReadSByte();
            var x = br.ReadSByte();
            return new Vector4(x / 127f, y / 127f, z / 127f, w / 127f);
        }

        private static Vector2 ReadShortUV(BinaryReaderEx br)
        {
            var u = br.ReadInt16();
            var v = br.ReadInt16();
            return new Vector2(u / 2048f, v / 2048f);
        }

        private static Vector4 Read10BitVector4(BinaryReaderEx br)
        {
            var vector = br.ReadInt32();
            var x = (vector << 22) >> 22;
            var y = (vector << 12) >> 22;
            var z = (vector << 2) >> 22;
            var w = (vector << 0) >> 30;
            return new Vector4(x / 511f, y / 511f, z / 511f, w);
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}