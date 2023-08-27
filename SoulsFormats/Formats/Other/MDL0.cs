using System.Collections.Generic;
using System.Drawing;
using System.Numerics;

namespace SoulsFormats.Other;

/// <summary>
///     A model format used in the original Otogi. Extension: .mdl
/// </summary>
public class MDL0 : SoulsFile<MDL0>
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public int Unk04;
    public int Unk08;
    public List<Bone> Bones;
    public List<ushort> Indices;
    public List<Vertex> VerticesA;
    public List<Vertex> VerticesB;
    public List<Vertex> VerticesC;
    public List<Struct6> Struct6s;
    public List<Material> Materials;
    public List<string> Textures;

    protected override void Read(BinaryReaderEx br)
    {
        br.ReadInt32(); // File size
        Unk04 = br.ReadInt32();
        Unk08 = br.ReadInt32();
        br.ReadInt32(); // Face count

        var boneCount = br.ReadInt32();
        var indexCount = br.ReadInt32();
        var vertexCountA = br.ReadInt32();
        var vertexCountB = br.ReadInt32();
        var vertexCountC = br.ReadInt32();
        var count6 = br.ReadInt32();
        var materialCount = br.ReadInt32();
        var textureCount = br.ReadInt32();

        var bonesOffset = br.ReadInt32();
        var indicesOffset = br.ReadInt32();
        var verticesOffsetA = br.ReadInt32();
        var verticesOffsetB = br.ReadInt32();
        var verticesOffsetC = br.ReadInt32();
        var offset6 = br.ReadInt32();
        var materialsOffset = br.ReadInt32();
        var texturesOffset = br.ReadInt32();

        br.Position = bonesOffset;
        Bones = new List<Bone>(boneCount);
        for (var i = 0; i < boneCount; i++)
            Bones.Add(new Bone(br));

        br.Position = indicesOffset;
        Indices = new List<ushort>(br.ReadUInt16s(indexCount));

        br.Position = verticesOffsetA;
        VerticesA = new List<Vertex>(vertexCountA);
        for (var i = 0; i < vertexCountA; i++)
            VerticesA.Add(new Vertex(br, VertexFormat.A));

        br.Position = verticesOffsetB;
        VerticesB = new List<Vertex>(vertexCountB);
        for (var i = 0; i < vertexCountB; i++)
            VerticesB.Add(new Vertex(br, VertexFormat.B));

        br.Position = verticesOffsetC;
        VerticesC = new List<Vertex>(vertexCountC);
        for (var i = 0; i < vertexCountC; i++)
            VerticesC.Add(new Vertex(br, VertexFormat.C));

        br.Position = offset6;
        Struct6s = new List<Struct6>(count6);
        for (var i = 0; i < count6; i++)
            Struct6s.Add(new Struct6(br));

        br.Position = materialsOffset;
        Materials = new List<Material>(materialCount);
        for (var i = 0; i < materialCount; i++)
            Materials.Add(new Material(br));

        br.Position = texturesOffset;
        Textures = new List<string>(textureCount);
        for (var i = 0; i < textureCount; i++)
            Textures.Add(br.ReadShiftJIS());
    }

    public List<int> Triangulate(Mesh mesh, List<Vertex> vertices)
    {
        var triangles = new List<int>();
        var flip = false;
        for (var i = mesh.StartIndex; i < mesh.StartIndex + mesh.IndexCount - 2; i++)
        {
            var vi1 = Indices[i];
            var vi2 = Indices[i + 1];
            var vi3 = Indices[i + 2];

            if (vi1 != vi2 && vi1 != vi3 && vi2 != vi3)
            {
                Vertex v1 = vertices[vi1 - mesh.StartVertex];
                Vertex v2 = vertices[vi2 - mesh.StartVertex];
                Vertex v3 = vertices[vi3 - mesh.StartVertex];
                var vertexNormal = Vector3.Normalize((v1.Normal + v2.Normal + v3.Normal) / 3);
                var faceNormal = Vector3.Normalize(Vector3.Cross(v2.Position - v1.Position, v3.Position - v1.Position));
                var angle = Vector3.Dot(faceNormal, vertexNormal) / (faceNormal.Length() * vertexNormal.Length());
                flip = angle < 0;

                if (!flip)
                {
                    triangles.Add(vi1);
                    triangles.Add(vi2);
                    triangles.Add(vi3);
                }
                else
                {
                    triangles.Add(vi3);
                    triangles.Add(vi2);
                    triangles.Add(vi1);
                }
            }

            flip = !flip;
        }

        return triangles;
    }

    public class Bone
    {
        public int ChildIndex;
        public List<Mesh> MeshesA;
        public List<Mesh> MeshesB;
        public List<MeshGroup> MeshesC;
        public int NextSiblingIndex;
        public int ParentIndex;
        public int PrevSiblingIndex;
        public Vector3 Rotation;
        public Vector3 Scale;
        public Vector3 Translation;
        public int Unk4C;

        internal Bone(BinaryReaderEx br)
        {
            Translation = br.ReadVector3();
            Rotation = br.ReadVector3();
            Scale = br.ReadVector3();
            ParentIndex = br.ReadInt32();
            ChildIndex = br.ReadInt32();
            NextSiblingIndex = br.ReadInt32();
            PrevSiblingIndex = br.ReadInt32();
            var meshCountA = br.ReadInt32();
            var meshCountB = br.ReadInt32();
            var meshCountC = br.ReadInt32();
            var meshesOffsetA = br.ReadInt32();
            var meshesOffsetB = br.ReadInt32();
            var meshesOffsetC = br.ReadInt32();
            Unk4C = br.ReadInt32();

            br.StepIn(meshesOffsetA);
            {
                MeshesA = new List<Mesh>(meshCountA);
                for (var i = 0; i < meshCountA; i++)
                    MeshesA.Add(new Mesh(br));
            }
            br.StepOut();

            br.StepIn(meshesOffsetB);
            {
                MeshesB = new List<Mesh>(meshCountB);
                for (var i = 0; i < meshCountB; i++)
                    MeshesB.Add(new Mesh(br));
            }
            br.StepOut();

            br.StepIn(meshesOffsetC);
            {
                MeshesC = new List<MeshGroup>(meshCountC);
                for (var i = 0; i < meshCountC; i++)
                    MeshesC.Add(new MeshGroup(br));
            }
            br.StepOut();
        }
    }

    public class MeshGroup
    {
        public short[] BoneIndices;
        public List<Mesh> Meshes;
        public byte Unk02;
        public byte Unk03;

        internal MeshGroup(BinaryReaderEx br)
        {
            var meshCount = br.ReadInt16();
            Unk02 = br.ReadByte();
            Unk03 = br.ReadByte();
            BoneIndices = br.ReadInt16s(4);
            var meshesOffset = br.ReadInt32();

            br.StepIn(meshesOffset);
            {
                Meshes = new List<Mesh>(meshCount);
                for (var i = 0; i < meshCount; i++)
                    Meshes.Add(new Mesh(br));
            }
            br.StepOut();
        }
    }

    public class Mesh
    {
        public int IndexCount;
        public byte MaterialIndex;
        public int StartIndex;
        public int StartVertex;
        public byte Unk01;
        public short VertexCount;

        internal Mesh(BinaryReaderEx br)
        {
            MaterialIndex = br.ReadByte();
            Unk01 = br.AssertByte(0, 1, 2);
            VertexCount = br.ReadInt16();
            IndexCount = br.ReadInt32();
            StartVertex = br.ReadInt32();
            StartIndex = br.ReadInt32();
        }
    }

    public enum VertexFormat
    {
        A,
        B,
        C
    }

    public class Vertex
    {
        public Color Color;
        public Vector3 Normal;
        public Vector3 Position;
        public float UnkFloatA;
        public float UnkFloatB;
        public short UnkShortA;
        public short UnkShortB;
        public Vector2[] UVs;

        public Vertex(Vector3 position, Vector3 normal)
        {
            Position = position;
            Normal = normal;
            UVs = new Vector2[2];
        }

        internal Vertex(BinaryReaderEx br, VertexFormat format)
        {
            Position = br.ReadVector3();
            Normal = Read11_11_10Vector3(br);
            Color = br.ReadRGBA();
            UVs = new Vector2[2];
            for (var i = 0; i < 2; i++)
                UVs[i] = br.ReadVector2();

            if (format >= VertexFormat.B)
            {
                UnkShortA = br.ReadInt16();
                UnkShortB = br.ReadInt16();
            }

            if (format >= VertexFormat.C)
            {
                UnkFloatA = br.ReadSingle();
                UnkFloatB = br.ReadSingle();
            }
        }

        private static Vector3 Read11_11_10Vector3(BinaryReaderEx br)
        {
            var vector = br.ReadInt32();
            var x = (vector << 21) >> 21;
            var y = (vector << 10) >> 21;
            var z = (vector << 0) >> 22;
            return new Vector3(x / (float)0b11_1111_1111, y / (float)0b11_1111_1111, z / (float)0b1_1111_1111);
        }
    }

    public class Struct6
    {
        public int BoneIndex;
        public Vector3 Position;
        public Vector3 Rotation;

        internal Struct6(BinaryReaderEx br)
        {
            Position = br.ReadVector3();
            Rotation = br.ReadVector3();
            BoneIndex = br.ReadInt32();
            br.AssertInt32(0);
        }
    }

    public class Material
    {
        public int DiffuseMapIndex;
        public int ReflectionMapIndex;
        public int ReflectionMaskIndex;
        public int Unk04;
        public int Unk08;
        public int Unk0C;
        public Vector4 Unk20;
        public Vector4 Unk30;
        public Vector4 Unk40;
        public float Unk60;
        public float Unk64;
        public float Unk68;
        public int Unk6C;

        internal Material(BinaryReaderEx br)
        {
            br.AssertInt32(0);
            Unk04 = br.ReadInt32();
            Unk08 = br.ReadInt32();
            Unk0C = br.ReadInt32();
            DiffuseMapIndex = br.ReadInt32();
            ReflectionMaskIndex = br.ReadInt32();
            ReflectionMapIndex = br.ReadInt32();
            br.AssertInt32(-1);
            Unk20 = br.ReadVector4();
            Unk30 = br.ReadVector4();
            Unk40 = br.ReadVector4();
            br.AssertPattern(0x10, 0x00);
            Unk60 = br.ReadSingle();
            Unk64 = br.ReadSingle();
            Unk68 = br.ReadSingle();
            Unk6C = br.ReadInt32();
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}