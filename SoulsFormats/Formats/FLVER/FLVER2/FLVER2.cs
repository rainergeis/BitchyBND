using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SoulsFormats;

/// <summary>
///     A model format used since Dark Souls 1. Extension: .flv, .flver
/// </summary>
public partial class FLVER2 : SoulsFile<FLVER2>, IFlver
{

    /// <summary>
    ///     Creates a FLVER with a default header and empty lists.
    /// </summary>
    public FLVER2()
    {
        Header = new FLVERHeader();
        Dummies = new List<FLVER.Dummy>();
        Materials = new List<Material>();
        GXLists = new List<GXList>();
        Bones = new List<FLVER.Bone>();
        Meshes = new List<Mesh>();
        BufferLayouts = new List<BufferLayout>();
    }

    /// <summary>
    ///     General values for this model.
    /// </summary>
    public FLVERHeader Header { get; set; }

    /// <summary>
    ///     Dummy polygons in this model.
    /// </summary>
    public List<FLVER.Dummy> Dummies { get; set; }

    /// <summary>
    ///     Materials in this model, usually one per mesh.
    /// </summary>
    public List<Material> Materials { get; set; }

    /// <summary>
    ///     Lists of GX elements referenced by materials in DS2 and beyond.
    /// </summary>
    public List<GXList> GXLists { get; set; }

    /// <summary>
    ///     Bones used by this model, may or may not be the full skeleton.
    /// </summary>
    public List<FLVER.Bone> Bones { get; set; }

    /// <summary>
    ///     Individual chunks of the model.
    /// </summary>
    public List<Mesh> Meshes { get; set; }

    /// <summary>
    ///     Layouts determining how to write vertex information.
    /// </summary>
    public List<BufferLayout> BufferLayouts { get; set; }

    /// <summary>
    ///     Unknown; only present in Sekiro.
    /// </summary>
    public SekiroUnkStruct SekiroUnk { get; set; }

    IReadOnlyList<FLVER.Dummy> IFlver.Dummies => Dummies;
    IReadOnlyList<IFlverMaterial> IFlver.Materials => Materials;
    IReadOnlyList<FLVER.Bone> IFlver.Bones => Bones;
    IReadOnlyList<IFlverMesh> IFlver.Meshes => Meshes;

    /// <summary>
    ///     Returns true if the data appears to be a FLVER.
    /// </summary>
    protected override bool Is(BinaryReaderEx br)
    {
        if (br.Length < 0xC)
            return false;

        var magic = br.GetASCII(0, 6);
        var endian = br.GetASCII(6, 2);
        br.BigEndian = endian == "B\0";
        var version = br.GetInt32(8);
        return magic == "FLVER\0" && version >= 0x20000;
    }

    /// <summary>
    ///     Reads FLVER data from a BinaryReaderEx.
    /// </summary>
    protected override void Read(BinaryReaderEx br)
    {
        br.BigEndian = false;

        Header = new FLVERHeader();
        br.AssertASCII("FLVER\0");
        Header.BigEndian = br.AssertASCII("L\0", "B\0") == "B\0";
        br.BigEndian = Header.BigEndian;

        // Gundam Unicorn: 0x20005, 0x2000E
        // ACVD: 
        // DS1: 2000B (PS3 o0700/1), 2000C, 2000D
        // DS2 NT: 2000F, 20010
        // DS2: 20010, 20009 (armor 9320)
        // SFS: 20010
        // BB:  20013, 20014
        // DS3: 20013, 20014
        // SDT: 2001A, 20016 (test chr)
        Header.Version = br.AssertInt32(0x20005, 0x20007, 0x20009, 0x2000B, 0x2000C, 0x2000D, 0x2000E, 0x2000F, 0x20010,
            0x20013, 0x20014, 0x20016, 0x2001A);

        var dataOffset = br.ReadInt32();
        br.ReadInt32(); // Data length
        var dummyCount = br.ReadInt32();
        var materialCount = br.ReadInt32();
        var boneCount = br.ReadInt32();
        var meshCount = br.ReadInt32();
        var vertexBufferCount = br.ReadInt32();

        Header.BoundingBoxMin = br.ReadVector3();
        Header.BoundingBoxMax = br.ReadVector3();

        br.ReadInt32(); // Face count not including motion blur meshes or degenerate faces
        br.ReadInt32(); // Total face count

        int vertexIndicesSize = br.AssertByte(0, 8, 16, 32);
        Header.Unicode = br.ReadBoolean();
        Header.Unk4A = br.ReadBoolean();
        br.AssertByte(0);

        Header.Unk4C = br.ReadInt32();

        var faceSetCount = br.ReadInt32();
        var bufferLayoutCount = br.ReadInt32();
        var textureCount = br.ReadInt32();

        Header.Unk5C = br.ReadByte();
        Header.Unk5D = br.ReadByte();
        br.AssertByte(0);
        br.AssertByte(0);

        br.AssertInt32(0);
        br.AssertInt32(0);
        Header.Unk68 = br.AssertInt32(0, 1, 2, 3, 4);
        br.AssertInt32(0);
        br.AssertInt32(0);
        br.AssertInt32(0);
        br.AssertInt32(0);
        br.AssertInt32(0);

        Dummies = new List<FLVER.Dummy>(dummyCount);
        for (var i = 0; i < dummyCount; i++)
            Dummies.Add(new FLVER.Dummy(br, Header.Version));

        Materials = new List<Material>(materialCount);
        var gxListIndices = new Dictionary<int, int>();
        GXLists = new List<GXList>();
        for (var i = 0; i < materialCount; i++)
            Materials.Add(new Material(br, Header, GXLists, gxListIndices));

        Bones = new List<FLVER.Bone>(boneCount);
        for (var i = 0; i < boneCount; i++)
            Bones.Add(new FLVER.Bone(br, Header.Unicode));

        Meshes = new List<Mesh>(meshCount);
        for (var i = 0; i < meshCount; i++)
            Meshes.Add(new Mesh(br, Header));

        var faceSets = new List<FaceSet>(faceSetCount);
        for (var i = 0; i < faceSetCount; i++)
            faceSets.Add(new FaceSet(br, Header, vertexIndicesSize, dataOffset));

        var vertexBuffers = new List<VertexBuffer>(vertexBufferCount);
        for (var i = 0; i < vertexBufferCount; i++)
            vertexBuffers.Add(new VertexBuffer(br));

        BufferLayouts = new List<BufferLayout>(bufferLayoutCount);
        for (var i = 0; i < bufferLayoutCount; i++)
            BufferLayouts.Add(new BufferLayout(br));

        var textures = new List<Texture>(textureCount);
        for (var i = 0; i < textureCount; i++)
            textures.Add(new Texture(br, Header));

        if (Header.Version >= 0x2001A)
            SekiroUnk = new SekiroUnkStruct(br);

        Dictionary<int, Texture> textureDict = SFUtil.Dictionize(textures);
        foreach (Material material in Materials) material.TakeTextures(textureDict);
        if (textureDict.Count != 0)
            throw new NotSupportedException("Orphaned textures found.");

        Dictionary<int, FaceSet> faceSetDict = SFUtil.Dictionize(faceSets);
        Dictionary<int, VertexBuffer> vertexBufferDict = SFUtil.Dictionize(vertexBuffers);
        foreach (Mesh mesh in Meshes)
        {
            mesh.TakeFaceSets(faceSetDict);
            mesh.TakeVertexBuffers(vertexBufferDict, BufferLayouts);
            mesh.ReadVertices(br, dataOffset, BufferLayouts, Header);
        }

        if (faceSetDict.Count != 0)
            throw new NotSupportedException("Orphaned face sets found.");
        if (vertexBufferDict.Count != 0)
            throw new NotSupportedException("Orphaned vertex buffers found.");
    }

    /// <summary>
    ///     Writes FLVER data to a BinaryWriterEx.
    /// </summary>
    protected override void Write(BinaryWriterEx bw)
    {
        bw.BigEndian = Header.BigEndian;
        bw.WriteASCII("FLVER\0");
        bw.WriteASCII(Header.BigEndian ? "B\0" : "L\0");
        bw.WriteInt32(Header.Version);

        bw.ReserveInt32("DataOffset");
        bw.ReserveInt32("DataSize");
        bw.WriteInt32(Dummies.Count);
        bw.WriteInt32(Materials.Count);
        bw.WriteInt32(Bones.Count);
        bw.WriteInt32(Meshes.Count);
        bw.WriteInt32(Meshes.Sum(m => m.VertexBuffers.Count));
        bw.WriteVector3(Header.BoundingBoxMin);
        bw.WriteVector3(Header.BoundingBoxMax);

        var trueFaceCount = 0;
        var totalFaceCount = 0;
        foreach (Mesh mesh in Meshes)
        {
            var allowPrimitiveRestarts = mesh.Vertices.Count < ushort.MaxValue;
            foreach (FaceSet faceSet in mesh.FaceSets)
                faceSet.AddFaceCounts(allowPrimitiveRestarts, ref trueFaceCount, ref totalFaceCount);
        }

        bw.WriteInt32(trueFaceCount);
        bw.WriteInt32(totalFaceCount);

        byte vertexIndicesSize = 0;
        if (Header.Version < 0x20013)
        {
            vertexIndicesSize = 16;
            foreach (Mesh mesh in Meshes)
            foreach (FaceSet fs in mesh.FaceSets)
                vertexIndicesSize = (byte)Math.Max(vertexIndicesSize, fs.GetVertexIndexSize());
        }

        bw.WriteByte(vertexIndicesSize);
        bw.WriteBoolean(Header.Unicode);
        bw.WriteBoolean(Header.Unk4A);
        bw.WriteByte(0);

        bw.WriteInt32(Header.Unk4C);

        bw.WriteInt32(Meshes.Sum(m => m.FaceSets.Count));
        bw.WriteInt32(BufferLayouts.Count);
        bw.WriteInt32(Materials.Sum(m => m.Textures.Count));

        bw.WriteByte(Header.Unk5C);
        bw.WriteByte(Header.Unk5D);
        bw.WriteByte(0);
        bw.WriteByte(0);

        bw.WriteInt32(0);
        bw.WriteInt32(0);
        bw.WriteInt32(Header.Unk68);
        bw.WriteInt32(0);
        bw.WriteInt32(0);
        bw.WriteInt32(0);
        bw.WriteInt32(0);
        bw.WriteInt32(0);

        foreach (FLVER.Dummy dummy in Dummies)
            dummy.Write(bw, Header.Version);

        for (var i = 0; i < Materials.Count; i++)
            Materials[i].Write(bw, i);

        for (var i = 0; i < Bones.Count; i++)
            Bones[i].Write(bw, i);

        for (var i = 0; i < Meshes.Count; i++)
            Meshes[i].Write(bw, i);

        var faceSetIndex = 0;
        foreach (Mesh mesh in Meshes)
        {
            for (var i = 0; i < mesh.FaceSets.Count; i++)
            {
                int indexSize = vertexIndicesSize;
                if (indexSize == 0)
                    indexSize = mesh.FaceSets[i].GetVertexIndexSize();

                mesh.FaceSets[i].Write(bw, Header, indexSize, faceSetIndex + i);
            }

            faceSetIndex += mesh.FaceSets.Count;
        }

        var vertexBufferIndex = 0;
        foreach (Mesh mesh in Meshes)
        {
            for (var i = 0; i < mesh.VertexBuffers.Count; i++)
                mesh.VertexBuffers[i].Write(bw, Header, vertexBufferIndex + i, i, BufferLayouts, mesh.Vertices.Count);
            vertexBufferIndex += mesh.VertexBuffers.Count;
        }

        for (var i = 0; i < BufferLayouts.Count; i++)
            BufferLayouts[i].Write(bw, i);

        var textureIndex = 0;
        for (var i = 0; i < Materials.Count; i++)
        {
            Materials[i].WriteTextures(bw, i, textureIndex);
            textureIndex += Materials[i].Textures.Count;
        }

        if (Header.Version >= 0x2001A)
            SekiroUnk.Write(bw);

        bw.Pad(0x10);
        for (var i = 0; i < BufferLayouts.Count; i++)
            BufferLayouts[i].WriteMembers(bw, i);

        bw.Pad(0x10);
        for (var i = 0; i < Meshes.Count; i++)
            Meshes[i].WriteBoundingBox(bw, i, Header);

        bw.Pad(0x10);
        var boneIndicesStart = (int)bw.Position;
        for (var i = 0; i < Meshes.Count; i++)
            Meshes[i].WriteBoneIndices(bw, i, boneIndicesStart);

        bw.Pad(0x10);
        faceSetIndex = 0;
        for (var i = 0; i < Meshes.Count; i++)
        {
            bw.FillInt32($"MeshFaceSetIndices{i}", (int)bw.Position);
            for (var j = 0; j < Meshes[i].FaceSets.Count; j++)
                bw.WriteInt32(faceSetIndex + j);
            faceSetIndex += Meshes[i].FaceSets.Count;
        }

        bw.Pad(0x10);
        vertexBufferIndex = 0;
        for (var i = 0; i < Meshes.Count; i++)
        {
            bw.FillInt32($"MeshVertexBufferIndices{i}", (int)bw.Position);
            for (var j = 0; j < Meshes[i].VertexBuffers.Count; j++)
                bw.WriteInt32(vertexBufferIndex + j);
            vertexBufferIndex += Meshes[i].VertexBuffers.Count;
        }

        bw.Pad(0x10);
        var gxOffsets = new List<int>();
        foreach (GXList gxList in GXLists)
        {
            gxOffsets.Add((int)bw.Position);
            gxList.Write(bw, Header);
        }

        for (var i = 0; i < Materials.Count; i++) Materials[i].FillGXOffset(bw, i, gxOffsets);

        bw.Pad(0x10);
        textureIndex = 0;
        for (var i = 0; i < Materials.Count; i++)
        {
            Material material = Materials[i];
            material.WriteStrings(bw, Header, i);

            for (var j = 0; j < material.Textures.Count; j++)
                material.Textures[j].WriteStrings(bw, Header, textureIndex + j);
            textureIndex += material.Textures.Count;
        }

        bw.Pad(0x10);
        for (var i = 0; i < Bones.Count; i++)
            Bones[i].WriteStrings(bw, Header.Unicode, i);

        var alignment = Header.Version <= 0x2000E ? 0x20 : 0x10;
        bw.Pad(alignment);
        if (Header.Version == 0x2000F || Header.Version == 0x20010)
            bw.Pad(0x20);

        var dataStart = (int)bw.Position;
        bw.FillInt32("DataOffset", dataStart);

        faceSetIndex = 0;
        vertexBufferIndex = 0;
        for (var i = 0; i < Meshes.Count; i++)
        {
            Mesh mesh = Meshes[i];
            for (var j = 0; j < mesh.FaceSets.Count; j++)
            {
                int indexSize = vertexIndicesSize;
                if (indexSize == 0)
                    indexSize = mesh.FaceSets[j].GetVertexIndexSize();

                bw.Pad(alignment);
                mesh.FaceSets[j].WriteVertices(bw, indexSize, faceSetIndex + j, dataStart);
            }

            faceSetIndex += mesh.FaceSets.Count;

            foreach (FLVER.Vertex vertex in mesh.Vertices)
                vertex.PrepareWrite();

            for (var j = 0; j < mesh.VertexBuffers.Count; j++)
            {
                bw.Pad(alignment);
                mesh.VertexBuffers[j].WriteBuffer(bw, vertexBufferIndex + j, BufferLayouts, mesh.Vertices, dataStart,
                    Header);
            }

            foreach (FLVER.Vertex vertex in mesh.Vertices)
                vertex.FinishWrite();

            vertexBufferIndex += mesh.VertexBuffers.Count;
        }

        bw.Pad(alignment);
        bw.FillInt32("DataSize", (int)bw.Position - dataStart);
        if (Header.Version == 0x2000F || Header.Version == 0x20010)
            bw.Pad(0x20);
    }

    /// <summary>
    ///     General metadata about a FLVER.
    /// </summary>
    public class FLVERHeader
    {

        /// <summary>
        ///     Creates a FLVERHeader with default values.
        /// </summary>
        public FLVERHeader()
        {
            BigEndian = false;
            Version = 0x20014;
            Unicode = true;
        }

        /// <summary>
        ///     If true FLVER will be written big-endian, if false little-endian.
        /// </summary>
        public bool BigEndian { get; set; }

        /// <summary>
        ///     Version of the format indicating presence of various features.
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        ///     Minimum extent of the entire model.
        /// </summary>
        public Vector3 BoundingBoxMin { get; set; }

        /// <summary>
        ///     Maximum extent of the entire model.
        /// </summary>
        public Vector3 BoundingBoxMax { get; set; }

        /// <summary>
        ///     If true strings are UTF-16, if false Shift-JIS.
        /// </summary>
        public bool Unicode { get; set; }

        /// <summary>
        ///     Unknown.
        /// </summary>
        public bool Unk4A { get; set; }

        /// <summary>
        ///     Unknown; I believe this is the primitive restart constant, but I'm not certain.
        /// </summary>
        public int Unk4C { get; set; }

        /// <summary>
        ///     Unknown.
        /// </summary>
        public byte Unk5C { get; set; }

        /// <summary>
        ///     Unknown.
        /// </summary>
        public byte Unk5D { get; set; }

        /// <summary>
        ///     Unknown.
        /// </summary>
        public int Unk68 { get; set; }
    }
}